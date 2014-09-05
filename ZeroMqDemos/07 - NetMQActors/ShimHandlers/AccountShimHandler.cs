using System;
using System.Linq;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using NetMQ.zmq;
using NetMQActors.Models;
using Newtonsoft.Json;

namespace NetMQActors
{
    /// <summary>
    /// This hander class is specific implementation that you would need
    /// to implement per actor. This essentially contains your commands/protocol
    /// and should deal with any command workload, as well as sending back to the
    /// other end of the PairSocket which calling code would receive by using the
    /// Actor classes various RecieveXXX() methods
    /// 
    /// This is a VERY simple protocol but it just demonstrates what you would need
    /// to do to implement your own Shim handler
    /// </summary>
    public class AccountShimHandler : IShimHandler
    {

        private void AmmendAccount(AccountAction action, Account account)
        {
            decimal currentAmount = account.Balance;
            account.Balance = action.TransactionType == TransactionType.Debit
                ? currentAmount - action.Amount
                : currentAmount + action.Amount;
        }

        public void Run(PairSocket shim, object[] args, CancellationToken token)
        {
            if (args == null || args.Count() != 1)
                throw new InvalidOperationException(
                    "Args were not correct, expected one argument");

            AccountAction accountAction = JsonConvert.DeserializeObject<AccountAction>(args[0].ToString());


            while (!token.IsCancellationRequested)
            {
                //Message for this actor/shim handler is expected to be 
                //Frame[0] : Command
                //Frame[1] : Payload
                //
                //Result back to actor is a simple echoing of the Payload, where
                //the payload is prefixed with "AMEND ACCOUNT"
                NetMQMessage msg = null;

                //this may throw NetMQException if we have disposed of the actor
                //end of the pipe, and the CancellationToken.IsCancellationRequested 
                //did not get picked up this loop cycle
                msg = shim.ReceiveMessage();

                if (msg == null)
                    break;

                if (msg[0].ConvertToString() == "AMEND ACCOUNT")
                {
                    string json = msg[1].ConvertToString();
                    Account account = JsonConvert.DeserializeObject<Account>(json);
                    AmmendAccount(accountAction, account);
                    shim.Send(JsonConvert.SerializeObject(account));
                }
                else
                {
                    throw NetMQException.Create("Unexpected command", 
                        ErrorCode.EFAULT);
                }
            }
        }
    }
}
