using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SSB4455.UdpTest {
	public class UdpServer {
		//以下默认都是私有的成员
		Socket socket; //目标socket
		EndPoint clientEnd; //客户端
		IPEndPoint ipEnd; //侦听端口
		string recvStr; //接收的字符串
		string sendStr; //发送的字符串
		byte[] recvData = new byte[1024]; //接收的数据，必须为字节
		byte[] sendData = new byte[1024]; //发送的数据，必须为字节
		int recvLen; //接收的数据长度
		Thread connectThread; //连接线程

		string outStr = "";

		void Start () {
			InitSocket ();
		}

		internal void InitSocket () {
			//定义侦听端口,侦听任何IP
			ipEnd = new IPEndPoint (IPAddress.Any, 8001);
			//定义套接字类型,在主线程中定义
			socket = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			//服务端需要绑定ip
			socket.Bind (ipEnd);
			//定义客户端
			IPEndPoint sender = new IPEndPoint (IPAddress.Any, 0);
			clientEnd = (EndPoint) sender;
			Debug.Log ("waiting for UDP dgram");
			outStr += "waiting for UDP dgram\n";

			//开启一个线程连接，必须的，否则主线程卡死
			connectThread = new Thread (new ThreadStart (SocketReceive));
			connectThread.Start ();
		}

		void SocketSend (string sendStr) {
			//清空发送缓存
			sendData = new byte[1024];
			//数据类型转换
			sendData = Encoding.UTF8.GetBytes (sendStr);
			//发送给指定客户端
			socket.SendTo (sendData, sendData.Length, SocketFlags.None, clientEnd);
		}

		void SocketSend (byte[] bytes) {
			socket.SendTo (bytes, bytes.Length, SocketFlags.None, clientEnd);
		}

		//服务器接收
		void SocketReceive () {
			//进入接收循环
			while (true) {
				//对data清零
				recvData = new byte[1024];
				//获取客户端，获取客户端数据，用引用给客户端赋值
				recvLen = socket.ReceiveFrom (recvData, ref clientEnd);
				outStr += "message from: " + clientEnd.ToString () + "\n";
				recvStr = Encoding.UTF8.GetString (recvData, 0, recvLen);

				var cmd = new CommandDecoder ();
				FrontCommand fc;
				bool result = cmd.Decode (buffer, out fc);

				Debug.Log ("message from: " + clientEnd.ToString () + "\n" + recvStr); //打印客户端信息 //输出接收到的数据
				outStr += recvStr + clientEnd.ToString () + "\n";
				//将接收到的数据经过处理再发送出去
				sendStr = "From Server reply: " + recvStr;
				SocketSend (recvData);
				SocketSend (sendStr);
			}
		}

		Socket server;
		Thread t;
		private void GetUdpReceiver () {
			server = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			server.Bind (new IPEndPoint (IPAddress.Parse (GetLocalIpAddress ("InterNetwork") [0]), CSGroupCarNetworkPortConfig.UDPPort)); //绑定端口号和IPIPAddress.Parse("192.168.31.229")
			t = new Thread (ReciveMsg); //开启接收消息线程
			t.Start ();
		}

		void ReciveMsg () {
			while (ReceivingData) {
				EndPoint point = new IPEndPoint (IPAddress.Any, 0); //用来保存发送方的ip和端口号
				byte[] buffer = new byte[1024];
				int length = server.ReceiveFrom (buffer, ref point); //接收数据报
				var cmd = new CommandDecoder ();
				FrontCommand fc;
				bool result = cmd.Decode (buffer, out fc);
				if (fc.Header.MainCommand == (ushort) GroupCarNetworkCommands.MainCommands.UDPConnect && fc.Header.SubCommand == (ushort) GroupCarNetworkCommands.UDPSubCommands.Response_AskHeartBeat) {
					InitTcpNetwork (point.ToString ().Split (':') [0]);
				}

			}
		}

		//连接关闭
		internal void SocketQuit () {
			//关闭线程
			if (connectThread != null) {
				connectThread.Interrupt ();
				connectThread.Abort ();
			}
			//最后关闭socket
			if (socket != null)
				socket.Close ();
			Debug.Log ("disconnect");
		}

		void OnApplicationQuit () {
			SocketQuit ();
		}

		public static System.Text.Encoding GetFileEncodeType (Byte[] buffer) {
			if (buffer != null && buffer.Length > 1 && buffer[0] >= 0xEF) {
				if (buffer[0] == 0xEF && buffer[1] == 0xBB) {
					return System.Text.Encoding.UTF8;
				} else if (buffer[0] == 0xFE && buffer[1] == 0xFF) {
					return System.Text.Encoding.BigEndianUnicode;
				} else if (buffer[0] == 0xFF && buffer[1] == 0xFE) {
					return System.Text.Encoding.Unicode;
				} else {
					return System.Text.Encoding.Default;
				}
			} else {
				return System.Text.Encoding.Default;
			}
		}
	}
}