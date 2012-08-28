//longbow.cs
//core of the Longbow application for Phyre

using System;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Cache;
using System.Web;
using System.Linq;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;

//using Mono.Terminal;

public class LongbowCore {
	
	public static string BuildNumber = Regex.Replace(System.IO.File.ReadAllText("longbow.vr"), "[^.0-9]", "");
	
	//version of the software
	public static string version = "0.1 (build "+BuildNumber+")";
	
	//Text displayed during startup
	public static string init_text = "Longbow v"+LongbowCore.version;
	
	//API URL base for sessions
	public static Uri api_url = new Uri("http://dev.phyre.im/api/longbow");
	
	public static void Main(string[] args){
		
		string username = "";
		bool askusername = false;
		
		LongbowToolkit Tools = new LongbowToolkit();
		LongbowInstanceData Data = new LongbowInstanceData();
		LongbowSessionData Session = new LongbowSessionData();
		LongbowServer Server = new LongbowServer();
		
		Console.WriteLine(LongbowCore.init_text);
		WebClient fetch = new WebClient();
		
		
		if (args.Length > 0){
			Session.Login = args[0];
			if (Session.Login.Length < 3){
			Console.WriteLine("Invalid username. Please try again.");
			askusername = true;
		}
		} else {
			askusername = true;
		}
		
		if (askusername){
			Console.WriteLine("Username:");
			Session.Login = Console.ReadLine();
		}
		
		Console.WriteLine("Attempting to log in as "+Session.Login+"...\nPassword: ");
		string password = Console.ReadLine();
		
		
		string parameters = "password="+password+"&username="+Session.Login;
		fetch.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
		
		//define our variables
		string		data = string.Empty;
		string		games_count = string.Empty;
		string		lberror	= string.Empty;
		string[]	game_entry = new string[2];
		int gameselection_int = 0;
		int chat_slice = 0;
		List<string> games_names = new List<string>(); 
		List<string> games_ids = new List<string>();
		
		string channel_line = string.Empty;
		 
		//end variables
		
		data = fetch.UploadString(api_url, parameters);
		XmlDocument xml = new XmlDocument();
		
		try {
			xml.LoadXml(data);
		}
			catch (Exception Ex)
		{
			Console.WriteLine("An unexpected response was received from the server. Is Longbow-CLI up to date?\n\nResponse: "+data);
			Environment.Exit(0);
		}
		
		XmlNodeList xnList = xml.SelectNodes("/root/error");
		foreach (XmlNode xn in xnList){
			lberror = xn["text"].InnerText;
			if (lberror.Length > 0){
				Console.WriteLine(lberror);
				Environment.Exit(0);
			}
		}
	
	
		//Console.WriteLine(data);
		xnList = xml.SelectNodes("/root/session");
	
		foreach (XmlNode xn in xnList){
			Session.SessionID = xn["luxSession"].InnerText;
			Session.Login = xn["luxLogin"].InnerText;
		}
		
	
		xnList = xml.SelectNodes("/root");
	
		foreach (XmlNode xn in xnList){
			games_count = xn["games_count"].InnerText;
		}
	
		xnList = xml.SelectNodes("/root/games/item");
	
		foreach (XmlNode xn in xnList){
			game_entry[0] = xn["gameid"].InnerText;
			game_entry[1] = xn["gamename"].InnerText;
			games_ids.Add(game_entry[0]);
			games_names.Add(game_entry[1]);
		}
	
		
		
		//XDocument doc = XDocument.Parse(data);
		
		//var session = doc.Root.Elements("session").Elements("luxSession").Select(element=>element.Value).ToList();
		//var gamescount = doc.Root.Elements("games_count").Select(element=>element.Value).ToList();
		
		//foreach(string value in gamescount){	Console.WriteLine(value); }
		//foreach(string value in session){	Console.WriteLine(value); }
		
		
		Console.WriteLine("\n\n\n\n\n\n\n\n\nLogged in as: {1} Games total: {0}", games_count, Session.Login);
		Tools.DrawDivider();
		for (int i = 0; i < games_ids.Count(); i++){
			Console.WriteLine("> "+(i+1)+"	(ID: "+games_ids[i]+")  	"+Tools.StripSlashes(games_names[i]));
		}
		
		Console.WriteLine("\n\nPlease enter the number (not ID) of the game you wish to join.");
		
		string gameselection = "";
		bool ChoseGame = false;
		
		while (!ChoseGame){
			gameselection = Console.ReadLine();
			try {
				gameselection_int = Convert.ToInt32(gameselection) - 1; //fencepost!
			} catch (FormatException e) {
				Console.WriteLine("That is not a number.");
			} finally {
				if (gameselection_int > games_ids.Count() || gameselection_int == -1){
					Console.WriteLine("Not a valid game! Try again.");
				} else {
					ChoseGame = true;
				}
			}
		}
		
		Data.ChannelID = Convert.ToInt32(games_ids[gameselection_int]);
		Data.ChannelName = games_names[gameselection_int];
		
		Console.WriteLine("Joining \"{0}\".\n", games_names[gameselection_int]);
		Tools.DrawDivider();
		
		
		//being lazy and plunking down the connection code here again
		
		parameters = "session="+Session.SessionID+"&username="+Session.Login+"&chatinit="+games_ids[gameselection_int];
		fetch.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
		data = fetch.UploadString(api_url, parameters);
		//Console.WriteLine(api_url+"?"+parameters);
		//return;
		xml.LoadXml(data);
		//Console.WriteLine(data);
		xnList = xml.SelectNodes("/root/item");
		
		string curtext = string.Empty;
		
		foreach (XmlNode xn in xnList){
			if (xn["editedcontent"].InnerText.Length > 0){
				curtext = xn["editedcontent"].InnerText;
			} else {
				curtext = xn["content"].InnerText;
			}
			
			Data.LastPost = xn["timestamp"].InnerText;
			
			channel_line = xn["username"].InnerText+": "+Tools.StripSlashes(curtext);
			
			if (xn["band"].InnerText == "3"){
					channel_line = "(OOC) "+channel_line;
				
			}
			
			channel_line = HttpUtility.HtmlDecode(channel_line);
			
			Data.ChannelBuffer.Add(channel_line);
		}
		
		//render the screen manually the first time
		Tools.DrawChat(Data.ChannelBuffer, Data.TemporaryBuffer);
		Tools.DrawInput(ref Data);
				
		//MAIN LOOP BEGIN
		bool loop = true;
		LongbowWorkerThread Worker = new LongbowWorkerThread();
		//Thread w = new Thread(Worker.UpdateChannel);
		//w.Start(Data.ChannelBuffer);
		ThreadPool.QueueUserWorkItem(o => Worker.UpdateChannel(ref Data, ref Session));
		ThreadPool.QueueUserWorkItem(o => Worker.ProcessPostQueue(ref Data, ref Session));
		
		ConsoleKeyInfo inText;
		int uCursorTop;
		int uCursorLeft;
		int ExcerptSize;
		string LeftBuffer;
		string RightBuffer;
		Data.vCursorPos = 2;
		Data.vBufferPos = 0;
		Data.ChatPrev.Add("");
		
		//sConsole.Write("> "+Data.ChatCurrent+" ");
		Console.SetCursorPosition(2, Console.CursorTop);
		
		while (loop) {
			
			uCursorLeft = Console.CursorLeft;
			uCursorTop = Console.CursorTop;
			
			inText = Console.ReadKey();
			
			
			if (inText.Key == ConsoleKey.Backspace) {
				if (uCursorLeft > 2){
					Console.SetCursorPosition(uCursorLeft-1, uCursorTop);
					LeftBuffer = Data.ChatCurrent.Substring(0,Data.vCursorPos-2);
					RightBuffer = Data.ChatCurrent.Substring(Data.vCursorPos-2);
					Data.ChatCurrent = LeftBuffer.Substring(0,LeftBuffer.Length-1) + RightBuffer;
					Console.Write(" "+uCursorLeft);
					Data.vCursorPos--;
					
					
				}
				
				Tools.DrawInput(ref Data);
			
			} else if (inText.Key == ConsoleKey.Enter) {
				Console.WriteLine(Session.Login+": "+Data.ChatCurrent);
				Data.TemporaryBuffer.Add(Session.Login+": "+Data.ChatCurrent);
				Tools.QueueNewPost(ref Data, Data.ChatCurrent);
				Data.ChatPrev.Add(Data.ChatCurrent);
				Data.ChatCurrent = "";
				if (Data.ChatPrev[0].Length > 7){
					Data.ChatPrev.Add(Data.ChatPrev[0]);
				}
				Data.ChatPrev[0] = "";
				Data.vCursorPos = 2;
				Data.vBufferPos = 0;
				ThreadPool.QueueUserWorkItem(o => Worker.UpdateChannel(ref Data, ref Session));
				Tools.DrawChat(Data.ChannelBuffer, Data.TemporaryBuffer);
				Tools.DrawInput(ref Data);
			
			} else if (inText.Key == ConsoleKey.LeftArrow || inText.Key == ConsoleKey.RightArrow) {
				
				if (inText.Key == ConsoleKey.LeftArrow) { 
					if (Data.vCursorPos > 2){
							Console.SetCursorPosition(Console.CursorLeft-1, uCursorTop);
						Data.vCursorPos--;
					}
					
				} else {
					if (Data.vCursorPos < Data.ChatCurrent.Length+2) {
						Console.SetCursorPosition(uCursorLeft+2, uCursorTop);
						Data.vCursorPos++;
					}
				}
				
				Tools.DrawInput(ref Data);
				
			} else if (inText.Key == ConsoleKey.UpArrow || inText.Key == ConsoleKey.DownArrow){
				Data.vCursorPos = 2;
				if (inText.Key == ConsoleKey.UpArrow){
					
					if (Data.vBufferPos == 0){
						Data.ChatPrev[0] = Data.ChatCurrent;
					}
					
					if (Data.vBufferPos < Data.ChatPrev.Count-1){
						Data.vBufferPos++;
						Data.ChatCurrent = Data.ChatPrev[Data.ChatPrev.Count-Data.vBufferPos];
						
					}
				} else {
					if (Data.vBufferPos > 0){
						Data.vBufferPos--;
						Data.ChatCurrent = Data.ChatPrev[Data.ChatPrev.Count-Data.vBufferPos-1];
					} else if (Data.vBufferPos == 0) {
						Data.ChatCurrent = Data.ChatPrev[0];
					}
				}
				
				Data.vCursorPos = Data.ChatCurrent.Length+2;
				
				Tools.DrawInput(ref Data);
				
			} else { //Process it as chantext input
			
			//Console.Write(inText.KeyChar);
				//Data.ChatCurrent = Data.ChatCurrent.Substring(0,Data.ChatCurrent.Length-1);
				LeftBuffer = Data.ChatCurrent.Substring(0,Data.vCursorPos-2);
				RightBuffer = Data.ChatCurrent.Substring(Data.vCursorPos-2);
				Data.ChatCurrent = LeftBuffer + inText.KeyChar + RightBuffer;
				
				//Data.ChatCurrent = Data.ChatCurrent + inText.KeyChar;
				Data.vCursorPos++;
				
				Tools.DrawInput(ref Data);
				
			}
			
			
		}
		
		
	}
	
}

// Account or login-related data, here
public class LongbowSessionData{
	public string Login;
	public string SessionID;
}

// Any kind of channel-related data should go in here. 
public class LongbowInstanceData {
	public string ChannelName;
	public int ChannelID;
	public string ChatCurrent = "";
	public string LastPost; //yes, we really are treating the timestamp as a string. PHP badger don't care.
	public List<string> ChatPrev = new List<string>();
	public List<string> ChannelBuffer = new List<string>();
	public List<string> ChannelBuffer_Last = new List<string>();
	public List<string> ChannelBufferStamps = new List<string>();
	public List<string> PostQueue = new List<string>(); //We need to loop through these at a steady rate to prevent getting ratelimited
	public List<string> TemporaryBuffer = new List<string>(); //So we can emulate instant chat sending
	public string TemporaryBuffer_Last;
	public int vCursorPos;
	public int vBufferPos;
}

//Talking to the server? Yeah boyeee.
public class LongbowServer {

	public void SendQueuedPost(LongbowInstanceData Data, LongbowSessionData Session){
		//Console.WriteLine(Data.ChatCurrent);return;
		WebClient fetch = new WebClient();
		Random thisRandom = new Random();
		int tag = thisRandom.Next(0,10000);
		string parameters = "postcontent="+HttpUtility.UrlEncode(Data.PostQueue[0])+"&session="+Session.SessionID+"&username="+Session.Login+"&gameid="+Data.ChannelID+"&tag="+tag;
		fetch.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
		try {
		fetch.UploadStringAsync(LongbowCore.api_url, parameters);
		} catch (WebException e) {
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Red;
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.Write("# It appears your connection to the server has been interrupted. (Tried to send post data)");
			Console.ResetColor();
			Console.Write("\n> ");
			Console.SetCursorPosition(2, Console.CursorTop);
		}
	}
	
	public void GetNewPosts(LongbowInstanceData Data, LongbowSessionData Session, WebClient fetch){
		string parameters = "session="+Session.SessionID+"&username="+Session.Login+"&gameid="+Data.ChannelID+"&postedsince="+Data.LastPost;
		
		LongbowWorkerThread Async = new LongbowWorkerThread();
		ThreadPool.QueueUserWorkItem(o => Async.GetChannelUpdates(ref Data, parameters));
		
		//ThreadPool.QueueUserWorkItem(o => Worker.UpdateChannel(ref Data.ChannelBuffer, ref Data.PostQueue, ref Data.ChatCurrent));
		
	}
	
}

public class LongbowWorkerThread {
	public int uCursorLeft;
	public int uCursorTop;
	public int nCursorLeft;
	public int nCursorTop;
	private LongbowToolkit Tools = new LongbowToolkit();
	
	public void ProcessPostQueue (ref LongbowInstanceData Data, ref LongbowSessionData Session){
		while (true){
			Thread.Sleep(500);
			if (Data.PostQueue.Count > 0){ //if we have a post to send
				LongbowServer Server = new LongbowServer();
				Server.SendQueuedPost(Data, Session);
				Data.PostQueue.RemoveAt(0);
				//Console.WriteLine("Fie");
			} else {
				//Console.WriteLine("Fum");
			}
		}
	}
	
	public void GetChannelUpdates(ref LongbowInstanceData Data, string parameters){
		
		try {
			WebClient fetch = new WebClient();
			fetch.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore); 
			fetch.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
			string NewData = fetch.UploadString(LongbowCore.api_url, parameters);
			LongbowToolkit Toolkit = new LongbowToolkit();
			Toolkit.AddNewPosts(ref Data, NewData);
			
		} catch (WebException e) {
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Red;
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.WriteLine("# It appears your connection to the server has been interrupted. (Tried to load post data.)");
			Console.WriteLine("Exception Message: " + e.InnerException);
			if(e.Status == WebExceptionStatus.ProtocolError) {
				Console.WriteLine("Status Code : {0}", ((HttpWebResponse)e.Response).StatusCode);
				Console.WriteLine("Status Description : {0}", ((HttpWebResponse)e.Response).StatusDescription);
			}
			Console.ResetColor();
			Console.Write("\n> ");
			Console.SetCursorPosition(2, Console.CursorTop);
		}
	}
	
	public void UpdateChannel(ref LongbowInstanceData Data, ref LongbowSessionData Session){
		uCursorLeft = Console.CursorLeft;
		uCursorTop = Console.CursorTop;
		bool ReloadChannel = false;
		
		//int thingy;
		WebClient Client = new WebClient();
		LongbowServer Server = new LongbowServer();
		
		while (true){
			Thread.Sleep(5000);
			//thingy = Data.ChannelBuffer.Count() - 2;
			
			for (int i = 0; i < Data.ChannelBuffer.Count; i++){
				if (!ReloadChannel){
					Tools.DrawChat(Data.ChannelBuffer, Data.TemporaryBuffer);
					Tools.DrawInput(ref Data);
					ReloadChannel = true;
					break;
				}
				
				if (Data.ChannelBuffer.Count > Data.ChannelBuffer_Last.Count){
					Tools.DrawChat(Data.ChannelBuffer, Data.TemporaryBuffer);
					Tools.DrawInput(ref Data);
					break;
				}
				
				if (Data.ChannelBuffer[i] != Data.ChannelBuffer_Last[i]){
					Tools.DrawChat(Data.ChannelBuffer, Data.TemporaryBuffer);
					Tools.DrawInput(ref Data);
					break;
				}
			}
			
			
			//Console.WriteLine("\n\n"+Data.ChannelBuffer[thingy]);
			Server.GetNewPosts(Data, Session, Client);
		}
	}
}

public class LongbowToolkit {

	public int SafeDraw(string Line){
		int pos;
		int MinNumLines = Line.Length / Console.WindowWidth;
		int NumLines = 0;
		string buffer;
		List<string> Completed = new List<string>();
		bool OOC = false;
		
		if (Line.IndexOf("(OOC) ") == 0){
			OOC = true;
		}
		
		while (Line.Length > 1){
			if (Line.Length > Console.WindowWidth){
				buffer = Line.Substring(0, Console.WindowWidth);
				pos = buffer.LastIndexOf(" ");
				if (pos > -1) {
					Completed.Add(Line.Substring(0, pos));
					Line = Line.Substring(pos+1);
				} else {
					Completed.Add(Line.Substring(0, Console.WindowWidth));
					Line = Line.Substring(Console.WindowWidth);
				}
			} else {
				Completed.Add(Line.Substring(0, Line.Length));
				Line = "";
			}
		}
		
		if (OOC){
			Console.ForegroundColor = ConsoleColor.Gray;
		}
		
		for (int i = 0; i < Completed.Count; i++){
		
			
			
			Console.WriteLine(Completed[i]);
			NumLines = i;
		}
		
		Console.ResetColor();
		
		return NumLines;
		
		
	}

	public void DrawDivider(int MaxWidth=0){
		string divider = "";
		int Width = 0;
		
		if (MaxWidth > 0 && MaxWidth < Console.WindowWidth){
			Width = MaxWidth;
		} else {
			Width = Console.WindowWidth;
		}
		
		for (int i = 0; i < Width-2; i++){
			divider = divider + "=";
		}
		
		divider = "#" + divider + "#"; 
		Console.WriteLine(divider);
	}

	public void DrawInput(ref LongbowInstanceData Data){ //takes the entire InstanceData object as argument because it needs several values
	
	
		string ChatCurrent = Data.ChatCurrent;
		string Blanker = "";
		int uCursorTop;
		int uCursorLeft;
		int Shift = 0;
		uCursorLeft = Console.CursorLeft;
		uCursorTop = Console.CursorTop;
		Console.SetCursorPosition(0, uCursorTop);
		int rCursorPos = (Console.WindowWidth - 5) - (Data.ChatCurrent.Length-Data.vCursorPos);
		if (rCursorPos < 2) {
			rCursorPos = 2;
			
		
		}
		
		//we need to assemble a "tray" of blank text to prevent flickering
		for (int i = 0; i < Console.WindowWidth-ChatCurrent.Length-2; i++) {
			Blanker = Blanker + " ";
		}
		
		//a little jiggering to account for editing posts which are bigger than the window
		if (Console.WindowWidth > ChatCurrent.Length+4) {
			Console.Write("> "+ChatCurrent+Blanker);
			//Console.SetCursorPosition(ChatCurrent.Length+2, uCursorTop);
			Console.SetCursorPosition(Data.vCursorPos, uCursorTop);
		} else {
			
			int chat_slice = ChatCurrent.Length-Console.WindowWidth+5; //+2 to account for the opening "> ", +3 to account for gap at end of line
			if (Data.vCursorPos-2 < chat_slice) {
				chat_slice = Data.vCursorPos;
				
				
					Console.Write("> "+ChatCurrent.Substring(chat_slice-2,Console.WindowWidth-5)+"  !");
				
				
			} else {
				Console.Write("> "+ChatCurrent.Substring(chat_slice)+"  *");
			}
			
			Console.SetCursorPosition(rCursorPos, uCursorTop);
		}
		
		
		
	}
	
	public void DrawChat(List<string> Chat, List<string> TempChat){
		//re-render the whole window
		
		int ExcerptSize;
		if (Chat.Count < 1) { return; }
		
		if (Chat.Count > 30){
			ExcerptSize = 0;
		} else {
			ExcerptSize = Chat.Count - 30;
		}
	
		for (int i = ExcerptSize; i < Chat.Count; i++){
			SafeDraw(Chat[i]);
			//Console.WriteLine(Chat[i]);
		}
		
		for (int i = 0; i < TempChat.Count; i++){
			SafeDraw(TempChat[i]);
			//Console.WriteLine(TempChat[i]);
		}
		
	}
	
	public void AddNewPosts(ref LongbowInstanceData Data, string NewData){
		
		Data.ChannelBuffer_Last.Clear();
		
		for (int i = 0; i < Data.ChannelBuffer.Count; i++){
			Data.ChannelBuffer_Last.Add(Data.ChannelBuffer[i]);
		}
	
		Data.TemporaryBuffer.RemoveRange(0, Data.TemporaryBuffer.Count);
		LongbowToolkit Tools = new LongbowToolkit();
		
		XmlDocument xml = new XmlDocument();
		xml.LoadXml(NewData);
		
		XmlNodeList xnList = xml.SelectNodes("/root/item");
		
		string curtext = string.Empty;
		string timestamp = string.Empty;
		string channel_line = string.Empty;
		
		foreach (XmlNode xn in xnList){
			if (xn["editedcontent"].InnerText.Length > 0){
				curtext = xn["editedcontent"].InnerText;
			} else {
				curtext = xn["content"].InnerText;
			}
			
			Data.LastPost = xn["timestamp"].InnerText;
			timestamp = Data.LastPost;
			
			channel_line = xn["username"].InnerText+": "+Tools.StripSlashes(curtext);
			
			if (xn["band"].InnerText == "3"){
				channel_line = "(OOC) "+channel_line;
			}
			
			channel_line = HttpUtility.HtmlDecode(channel_line);
			if (!Data.ChannelBufferStamps.Exists(item => item == timestamp)){
				Data.ChannelBuffer.Add(channel_line);
				Data.ChannelBufferStamps.Add(timestamp);
			}
		}
		
	}
	
	public void QueueNewPost(ref LongbowInstanceData Data, string Buffer){
		Data.PostQueue.Add(Buffer);
	}
	
	/// <summary>
	/// Un-quotes a quoted string
	/// </summary>
	/// <param name="InputTxt">Text string need to be escape with slashes</param>	
	public string StripSlashes(string InputTxt)
	{
	    // List of characters handled:
	    // \000 null
	    // \010 backspace
	    // \011 horizontal tab
	    // \012 new line
	    // \015 carriage return
	    // \032 substitute
	    // \042 double quote
	    // \047 single quote
	    // \134 backslash
	    // \140 grave accent

	    string Result = InputTxt;

	    try
	    {
		Result = System.Text.RegularExpressions.Regex.Replace(InputTxt, @"(\\)([\000\010\011\012\015\032\042\047\134\140])", "$2");
	    }
	    catch (Exception Ex)
	    {
		// handle any exception here
		Console.WriteLine(Ex.Message);
	    }

	    return Result;
	}
}

static class Extensions
{
        public static IList<T> Clone<T>(this IList<T> listToClone) where T: ICloneable
        {
                return listToClone.Select(item => (T)item.Clone()).ToList();
        }
}

