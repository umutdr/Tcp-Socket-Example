using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpSocket1
{
    public static class SocketHelper
    {
        public static bool IsDisconnected(this Socket client)
        {
            // https://social.msdn.microsoft.com/Forums/en-US/c857cad5-2eb6-4b6c-b0b5-7f4ce320c5cd/c-how-to-determine-if-a-tcpclient-has-been-disconnected?forum=netfxnetcom#eae2a1ab-ac9a-4e5a-93a8-49f5483ff797
            // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.poll?view=net-5.0
            bool isWriteable = client.Poll(0, SelectMode.SelectWrite);
            bool isReadable = client.Poll(0, SelectMode.SelectRead);
            bool isError = client.Poll(0, SelectMode.SelectError);

            if (isReadable)
            {
                if (client.Receive(new byte[1], SocketFlags.Peek) == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
