//longbow.cs
//core of the Longbow application for Phyre

using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Web;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
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
			if (username.Length < 3){
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
		List<string> games_names = new List<string>(); 
		List<string> games_ids = new List<string>();
		
		string channel_line = string.Empty;
		 
		//end variables
		
		data = fetch.UploadString(api_url, parameters);
		
		XmlDocument xml = new XmlDocument();
		xml.LoadXml(data);
		
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
		Console.WriteLine("|=====================================|\n");
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
		
		Console.WriteLine("Joining \"{0}\".\n|=============|", games_names[gameselection_int]);
		
		
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
			
			channel_line = xn["username"].InnerText+": "+Tools.StripSlashes(curtext);
			
			if (xn["band"].InnerText == "3"){
				channel_line = "(OOC) "+channel_line;
			}
			
			channel_line = HttpUtility.HtmlDecode(channel_line);
			
			Console.WriteLine(channel_line);
			Data.ChannelBuffer.Add(channel_line);
		}
		Console.WriteLine("\n");
		
		//MAIN LOOP BEGIN
		bool loop = true;
		LongbowWorkerThread Worker = new LongbowWorkerThread();
		//Thread w = new Thread(Worker.UpdateChannel);
		//w.Start(Data.ChannelBuffer);
		ThreadPool.QueueUserWorkItem(o => Worker.UpdateChannel(ref Data.ChannelBuffer, ref Data.ChatBuffer, ref Data.ChatCurrent));
		
		
		ConsoleKeyInfo inText;
		int uCursorTop;
		int uCursorLeft;
		int ExcerptSize;
		
		Console.Write("> "+Data.ChatCurrent+" ");
		Console.SetCursorPosition(2, Console.CursorTop);
		
		while (loop) {
			
			uCursorLeft = Console.CursorLeft;
			uCursorTop = Console.CursorTop;
			
			inText = Console.ReadKey();
			
			
			if (inText.Key == ConsoleKey.Backspace) {
				if (uCursorLeft > 2){
					Console.SetCursorPosition(uCursorLeft-1, uCursorTop);
					Data.ChatCurrent = Data.ChatCurrent.Substring(0,Data.ChatCurrent.Length-1);
					Console.Write(" ");
				}
			
			} else if (inText.Key == ConsoleKey.Enter) {
				Server.SendPost(Data, Session, fetch);
				Console.WriteLine(Data.ChatCurrent);
				Data.ChannelBuffer.Add(Data.ChatCurrent);
				Data.ChatCurrent = "";
			
			} else {
			
			Console.Write(inText.KeyChar);
				Data.ChatCurrent = Data.ChatCurrent + inText.KeyChar;
			}
			
			Console.SetCursorPosition(0, uCursorTop);
			Console.Write("> "+Data.ChatCurrent+" ");
			Console.SetCursorPosition(Data.ChatCurrent.Length+2, uCursorTop);
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
	public string LastPost;
	public List<string> ChatBuffer = new List<string>();
	public List<string> ChannelBuffer = new List<string>();
}

//Talking to the server? Yeah boyeee.
public class LongbowServer {
	public void SendPost(LongbowInstanceData Data, LongbowSessionData Session, WebClient fetch){
		//Console.WriteLine(Data.ChatCurrent);return;
		Random thisRandom = new Random();
		int tag = thisRandom.Next(0,10000);
		string parameters = "postcontent="+HttpUtility.UrlPathEncode(Data.ChatCurrent)+"&session="+Session.SessionID+"&username="+Session.Login+"&gameid="+Data.ChannelID+"&tag="+tag;
		fetch.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
		fetch.UploadStringAsync(LongbowCore.api_url, parameters);
	
	}
	
	public void GetNewPosts(LongbowInstanceData Data, LongbowSessionData Session, WebClient fetch){
		string parameters = "session="+Session.SessionID+"&username="+Session.Login+"&gameid="+Data.ChannelID+"&tag="+tag;
		fetch.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
		fetch.UploadStringAsync(LongbowCore.api_url, parameters);
	}
}

public class LongbowWorkerThread {
	public int uCursorLeft;
	public int uCursorTop;
	public int nCursorLeft;
	public int nCursorTop;
	private LongbowToolkit Tools = new LongbowToolkit();

	
	
	public void UpdateChannel(ref List<string> ChannelBuffer, ref List<string> ChatBuffer, ref string ChatCurrent){
		uCursorLeft = Console.CursorLeft;
		uCursorTop = Console.CursorTop;
		while (true){
			Thread.Sleep(5000); 
			ChannelBuffer.Add("Dummy Teckst");
			Tools.DrawChat(ChannelBuffer);
			Tools.DrawInput(ChatCurrent);
		}
	}
}

public class LongbowToolkit {

	public void DrawInput(string ChatCurrent){
	
		int uCursorTop;
		int uCursorLeft;
		uCursorLeft = Console.CursorLeft;
		uCursorTop = Console.CursorTop;
		Console.SetCursorPosition(0, uCursorTop);
		Console.Write("> "+ChatCurrent+" ");
		Console.SetCursorPosition(ChatCurrent.Length+2, uCursorTop);
	}
	
	public void DrawChat(List<string> OurChat){
		//re-render the whole window
			
		int ExcerptSize;
	
		if (OurChat.Count > 30){
			ExcerptSize = 0;
		} else {
			ExcerptSize = OurChat.Count - 30;
		}
	
		for (int i = ExcerptSize; i < OurChat.Count; i++){
			Console.WriteLine(OurChat[i]);
		}
			
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
