using System.Threading;

namespace Musoq.Schema
{
    public class InterCommunicator
    {
        public CancellationToken EndWorkToken { get; }

        public InterCommunicator(CancellationToken endWorkToken)
        {
            EndWorkToken = endWorkToken;
        }

        public static InterCommunicator Empty => new InterCommunicator(CancellationToken.None);
    }
}
