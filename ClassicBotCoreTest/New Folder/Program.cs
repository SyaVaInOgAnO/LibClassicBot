﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using LibClassicBot;
using LibClassicBot.Drawing;
using LibClassicBot.Events;
using LibClassicBot.Remote.Events;

namespace LibClassicBotTest
{
	class Program
	{
		const string LoginSecureUri = "https://minecraft.net/login";
		const string WrongUsernameOrPasswordMessage = "Oops, unknown username or password.";
		static Cookie PlaySession = null;

		static string UploadString( string uri, string dataToPost ) {
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create( uri );
			request.CookieContainer = new CookieContainer();
			if(PlaySession != null) request.CookieContainer.Add(PlaySession);
			if(dataToPost != null) {
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				byte[] data = Encoding.UTF8.GetBytes( dataToPost );
				request.ContentLength = data.Length;
				using( Stream stream = request.GetRequestStream() ) {
					stream.Write( data, 0, data.Length ); }
			}
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			foreach(Cookie c in response.Cookies) {
				if(c.Name == "PLAY_SESSION") PlaySession = c;
			}
			using( Stream stream = response.GetResponseStream() )
				using( StreamReader reader = new StreamReader( stream ) ) {
				return reader.ReadToEnd();
			}
		}
		const string user = "_BotN64_", pass = "Lu1g1fan";
		public static void Main(string[] args)
		{
			string loginString = String.Format("username={0}&password={1}", user, pass);
			string loginResponse = UploadString( LoginSecureUri, loginString );

			if( loginResponse.Contains( WrongUsernameOrPasswordMessage ) ) { }
			else if( loginResponse.Contains(user) ){ /*Success..*/ }
			string tt = UploadString("http://minecraft.net/classic/play/340f28a81e01e33b89af1d9143584182", null);
			if(tt.Contains("mppass")) Console.WriteLine(":D");
			
			AppDomain.CurrentDomain.UnhandledException += UnhandledException;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Welcome to LibClassicBot beta.");
			Console.WriteLine("Below is a list of commands and how to use them");
			Console.WriteLine(".position - Announces the position of the bot in the map.");
			Console.WriteLine(".allow <user> - Adds a user to a list of allowed operators.");
			Console.WriteLine(".disallow <user> - Removes a user from the list of allowed operators.");
			Console.WriteLine(".say <message> - Makes the bot send the specified message.");
			Console.WriteLine(".players - Lists the visibile players to the bot in the current map.");
			Console.WriteLine(".move <x,y,z> - Moves the bot to the specified coordinates.");
			Console.WriteLine(".place <x,y,z> - Attempts to place a block at the specified coordinates.");
			Console.WriteLine(".haspaid <username> - Announces if a user has paid or not.");
			Console.ResetColor();
			
			Console.WriteLine("Enter the username to be used by the bot: (Minecraft account)");
			string username = Console.ReadLine();
			Console.WriteLine("Enter the password to be used by the bot: (Minecraft account)");
			string password = Console.ReadLine();
			Console.WriteLine("Enter the address of the server to connect to: ");
			string hash = Console.ReadLine();
			if(!hash.StartsWith("http"))
			{
				if(hash.StartsWith("minecraft")) hash = "http://"+hash;
				else hash = "http://minecraft.net/classic/play/" + hash;
			}

			ClassicBot Bot1 = new ClassicBot(username,password,hash,"operators.txt");
			Bot1.Events.ChatMessage += Bot1_ChatMessage;
			Bot1.Events.GotKicked += Bot1_GotKicked;
			Bot1.Events.PacketReceived += Bot1_PacketReceived;
			Bot1.Events.BotException += Bot1_SocketError;
			Bot1.Events.PlayerMoved += Bot1_PlayerMoved;
			Bot1.RemoteServerEvents.RemoteSessionStarted += RemoteSessionStarted;
			Bot1.RemoteServerEvents.RemoteUserLoggedIn += RemoteUserLoggedIn;
			Bot1.RemoteServerEvents.RemoteSessionEnded += RemoteSessionEnded;
			
			#region Plugins
			
			ClassicBot.CommandDelegate PositionCommand = delegate(string Line)
			{
				Bot1.SendMessagePacket(String.Format("Positon in world is at {0},{1},{2}.", Bot1.X, Bot1.Y, Bot1.Z));
			};
			Bot1.RegisteredCommands.Add("position",PositionCommand);
			
			ClassicBot.CommandDelegate AddOpCommand = delegate(string Line)
			{
				string[] full = Bot1.GetMessage(Line).Split(new char[] {' '}, 2);
				Bot1.AddOperator(full[1], true);
				Bot1.SendMessagePacket("Allowed user: " + full[1]);
			};
			Bot1.RegisteredCommands.Add("allow",AddOpCommand);
			
			ClassicBot.CommandDelegate RemoveOpCommand = delegate(string Line)
			{
				string[] full = Bot1.GetMessage(Line).Split(new char[] {' '}, 1);
				Bot1.RemoveOperator(full[1], true);
				Bot1.SendMessagePacket("Disallowed user: "+ full[1]);
			};
			Bot1.RegisteredCommands.Add("disallow",RemoveOpCommand);

			ClassicBot.CommandDelegate SayCommand = delegate(string Line)
			{
				string[] full = Bot1.GetMessage(Line).Split(new char[] {' '}, 2);
				Bot1.SendMessagePacket(full[1]);
			};
			Bot1.RegisteredCommands.Add("say",SayCommand);
			
			ClassicBot.CommandDelegate PlayersCommand = delegate(string Line)
			{
				List<string> Names = new List<string>();
				foreach(Player player in Bot1.Players.Values)
				{
					Names.Add(player.Name);
				}
				string output = String.Join(",",Names.ToArray());
				Bot1.SendLongChat("Players in current world: " + output);
			};
			Bot1.RegisteredCommands.Add("players",PlayersCommand);

			ClassicBot.CommandDelegate MoveCommand = delegate(string Line)
			{
				string[] full = Bot1.GetMessage(Line).Split(new char[] {' '}, 2);
				string[] coords = full[1].Split(new char[] { ',' }, 3);
				Bot1.SendPositionPacket(Convert.ToInt16(coords[0]), Convert.ToInt16(coords[1]), Convert.ToInt16(coords[2]));
			};
			Bot1.RegisteredCommands.Add("move",MoveCommand);
			
			ClassicBot.CommandDelegate PlaceCommand = delegate(string Line)
			{
				string[] full = Bot1.GetMessage(Line).Split(new char[] {' '}, 2);
				string[] coords = full[1].Split(new char[] { ',' }, 3);
				Bot1.SendBlockPacket(Convert.ToInt16(coords[0]), Convert.ToInt16(coords[1]), Convert.ToInt16(coords[2]), 1, 29);
			};
			Bot1.RegisteredCommands.Add("place",PlaceCommand);
			
			ClassicBot.CommandDelegate HasPaidCommand = delegate(string Line)
			{
				string[] LineSplit = Extensions.StripColors(Line).Split(' ');
				try
				{
					bool b;
					WebClient c = new WebClient();
					string response = c.DownloadString("https://minecraft.net/haspaid.jsp?user="+LineSplit[2]);
					if(Boolean.TryParse(response, out b)) Bot1.SendMessagePacket(response);
				}
				catch(WebException ex)
				{
					if (ex.Status == WebExceptionStatus.ProtocolError)
					{
						HttpWebResponse response = ex.Response as HttpWebResponse;
						if (response != null)
							Bot1.SendMessagePacket("minecraft.net returned: " + (int)response.StatusCode + " " +response.StatusCode.ToString());
					}
					else
						Bot1.SendMessagePacket("Unhandled error occured: "+ex.Status.ToString());
				}
			};
			Bot1.RegisteredCommands.Add("haspaid",HasPaidCommand);
			
			ClassicBot.CommandDelegate FollowCommand = delegate(string Line)
			{
				string[] full = Bot1.GetMessage(Line).Split(new char[] {' '}, 2);
				personfollowed = full[1];
				Bot1.SendMessagePacket("Following user "+full[1]);
			};
			Bot1.RegisteredCommands.Add("follow",FollowCommand);
			
			ClassicBot.CommandDelegate CuboidCommand = delegate(string Line)
			{
				Cuboid cuboid = new Cuboid();
				Bot1.QueuedDrawers.Enqueue(cuboid);
			};
			Bot1.RegisteredCommands.Add("cuboid",CuboidCommand);
			
			ClassicBot.CommandDelegate PyramidCommand = delegate(string Line)
			{
				Pyramid pyramid = new Pyramid();
				Bot1.QueuedDrawers.Enqueue(pyramid);
			};
			Bot1.RegisteredCommands.Add("pyramid",PyramidCommand);
			
			ClassicBot.CommandDelegate AbortCommand = delegate(string Line)
			{
				Bot1.CancelDrawer();
			};
			Bot1.RegisteredCommands.Add("abort",AbortCommand);
			
			ClassicBot.CommandDelegate SpeedCommand = delegate(string Line)
			{
				string[] full = Bot1.GetMessage(Line).Split(new char[] {' '}, 2);
				Bot1.CuboidSleepTime = Int32.Parse(full[1]);
			};
			Bot1.RegisteredCommands.Add("speed",SpeedCommand);
			#endregion
			
			StaticBot1 = Bot1;
			Bot1.Start(false);
			
		loop:
			{
				string response = Console.ReadLine();
				Bot1.SendMessagePacket(response);
				goto loop;
			}
		}

		static string personfollowed = String.Empty;
		
		static void Bot1_PlayerMoved(object sender, PositionEventArgs e)
		{
			string name = e.player.Name;
			if(name.StartsWith("&")) name = name.Substring(2);
			if(personfollowed == name)
			{
				Console.WriteLine("Moving to "+e.player.X +","+ e.player.Y +","+e.player.Z);
				StaticBot1.SendPositionPacket(e.player.X, e.player.Y, e.player.Z, e.player.Yaw, e.player.Pitch);
			}
		}

		/// <summary>
		/// This catches all unhandled exceptions and logs them to error.txt. Useful for finding issues, although I really do hope
		/// that the bot is stable enough that this shouldn't need to be raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception actualException = (Exception)e.ExceptionObject; //Get actual exception.
			System.IO.File.WriteAllText("unhandlederror.txt","");
			System.IO.File.AppendAllText("unhandlederror.txt","Type of Exception - " + actualException.GetType() + Environment.NewLine);
			System.IO.File.AppendAllText("unhandlederror.txt","StackTrace - " + actualException.StackTrace + Environment.NewLine);
			System.IO.File.AppendAllText("unhandlederror.txt","Message - " + actualException.Message + Environment.NewLine);
			System.IO.File.AppendAllText("unhandlederror.txt","Source - " + actualException.Source + Environment.NewLine);
			//Don't bother with innerexception, seems never to be raised.
		}

		static void RemoteSessionEnded(object sender, SessionEndedEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Session with {0} ended.",e.Username);
			Console.ResetColor();
		}

		static void RemoteUserLoggedIn(object sender, RemoteLoginEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Session {0} user identified as {1}",e.RemoteEndPoint,e.Username);
			Console.ResetColor();
		}

		static void RemoteSessionStarted(object sender, SessionStartedEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Session started with {0}",e.RemoteEndPoint);
			Console.ResetColor();
		}
		
		static ClassicBot StaticBot1;
		
		static void LogError(string errormessage)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(errormessage);
			Console.ResetColor();
		}
		
		static void Bot1_SocketError(object sender, BotExceptionEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Bot Error: "+e.Output);
			Console.ResetColor();
		}

		static void Bot1_GotKicked(object sender, KickedEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("Bot kicked: ");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(e.Reason);
			Console.ResetColor();
		}
		
		static void Bot1_PacketReceived(object sender, PacketEventArgs e)
		{
			//Console.WriteLine(e.PacketType);
		}

		static void Bot1_ChatMessage(object sender, MessageEventArgs e)
		{
			AppendLog("&fBot: "+e.Line+"&");
			StaticBot1.MessageAllRemoteClients(e.Line);
		}
		
		/// <summary>
		/// Determines whether or not the given character is the character that follows an and symbol, to make up a minecraft colour code.
		/// </summary>
		/// <param name="c">The character to check.</param>
		/// <param name="color">The ConsoleColor that is used by AppendLog().</param>
		/// <returns>True if the given character was valid, and also returns a ConsoleColor with the equivalent of the Minecraft colour.
		/// By default, it will return Consolecolor.White.</returns>
		public static bool IsColourCodeChar(char c, out ConsoleColor color)
		{
			switch(c)
			{
					case '0': color = ConsoleColor.Black; return true; //Black
					case '1': color = ConsoleColor.DarkBlue; return true; //Dark Blue / Navy
					case '2': color = ConsoleColor.DarkGreen; return true; //Dark Green / Green
					case '3': color = ConsoleColor.DarkCyan; return true; //Dark Cyan / Teal
					case '4': color = ConsoleColor.DarkRed; return true; //Dark Red / Maroon
					case '5': color = ConsoleColor.DarkMagenta; return true; //Dark Magenta / Purple
					case '6': color = ConsoleColor.DarkYellow; return true; //Dark Yellow / Olive
					case '7': color = ConsoleColor.DarkGray; return true; //Dark Gray / Silver
					case '8': color = ConsoleColor.Gray; return true; //Gray
					case '9': color = ConsoleColor.Blue; return true; //Blue
					case 'a': color = ConsoleColor.Green; return true; //Green / Lime
					case 'b': color = ConsoleColor.Cyan; return true; //Cyan / Aqua
					case 'c': color = ConsoleColor.Red; return true; //Red
					case 'd': color = ConsoleColor.Magenta; return true; //Magenta
					case 'e': color = ConsoleColor.Yellow; return true; //Yellow
					case 'f': color = ConsoleColor.White; return true; //White
					default: color = ConsoleColor.White; return false; //Unknown. Go back to default
			}
		}

		public static void AppendLog(string input)
		{
			if(!input.EndsWith("&")) input += "&"; //Since I can't seem to do it any other way.
			ConsoleColor currentColor = ConsoleColor.White;
			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] == '&' && i < input.Length - 1)
				{
					i++;//Move to colour code character.
					if(IsColourCodeChar(input[i],out currentColor))
					{
						i++;
						StringBuilder sb = new StringBuilder();
						while (input[i] != '&') //Look at fixing this up so we don't always have to append & to the end..
						{
							sb.Append(input[i]);
							i++;
						}
						i--; //Move back a character, as otherwise we go one too far and consume the next &.
						Console.ForegroundColor = currentColor;
						Console.Write(sb);
					}
				}
			}
			Console.Write(Environment.NewLine); //Write a new line to end the current message.
			Console.ResetColor(); //Go back to default colour
		}
	}
}