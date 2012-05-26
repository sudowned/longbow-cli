//longbow.cs
//core of the Longbow application for Phyre

using System;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

//using Mono.Terminal;

public class LongbowCore {
	//version of the software
	public static string version = "0.1";
	
	//Text displayed during startup
	public static string init_text = "Longbow v"+LongbowCore.version;
	
	//API URL base for sessions
	public static string api_url = "http://dev.phyre.im/api/longbow";
	
	public static void Main(string[] args){
		
		string username = "";
		bool askusername = false;
		
		LongbowToolkit Tools = new LongbowToolkit();
		
		Console.WriteLine(LongbowCore.init_text);
		WebClient fetch = new WebClient();
		
		if (args.Length > 0){
			username = args[0];
			if (username.Length < 3){
			Console.WriteLine("Invalid username. Please try again.");
			askusername = true;
		}
		} else {
			askusername = true;
		}
		
		if (askusername){
			Console.WriteLine("Username:");
			username = Console.ReadLine();
		}
		
		Console.WriteLine("Attempting to log in as "+username+"...\nPassword: ");
		string password = Console.ReadLine();
		
		
		string parameters = "password="+password+"&username="+username;
		fetch.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
		
		//define our variables
		string		data,session = string.Empty;
		string		games_count = string.Empty;
		string		lberror	= string.Empty;
		string[]	game_entry = new string[2];
		int gameselection_int = 0;
		List<string> games_names = new List<string>(); 
		List<string> games_ids = new List<string>();
		
		string channel_line = string.Empty;
		List<string> channel_lines = new List<string>();
		 
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
			session = xn["luxSession"].InnerText;
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
		
		
		Console.WriteLine("\n\n\n\n\n\n\n\n\nLogged in as: {1} Games total: {0}", games_count, username);
		Console.WriteLine("|=====================================|\n");
		for (int i = 0; i < games_ids.Count(); i++){
			Console.WriteLine("> "+i+"	(ID: "+games_ids[i]+")	"+Tools.StripSlashes(games_names[i]));
		}
		
		Console.WriteLine("\n\nPlease enter the number (not ID) of the game you wish to join.");
		
		string gameselection = "";
		gameselection = Console.ReadLine();
		
		try {
			gameselection_int = Convert.ToInt32(gameselection);
		} catch (FormatException e) {
			Console.WriteLine("That is not a number.");
		} finally {
			if (gameselection_int > games_ids.Count()){
				Console.WriteLine("Not a valid game! Exiting.");
				Environment.Exit(0);
			}
		}
		
		Console.WriteLine("Joining \"{0}\".\n|=============|", games_names[gameselection_int]);
		
		
		//being lazy and plunking down the connection code here again
		
		parameters = "password="+password+"&username="+username;
		
		parameters = "session="+session+"&username="+username+"&chatinit="+games_ids[gameselection_int];
		fetch.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
		data = fetch.UploadString(api_url, parameters);
		
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
			Console.WriteLine(channel_line);
		}
		Console.WriteLine("");
		
		//MAIN LOOP BEGIN
		bool loop = true;
		LongbowWorkerThread Worker = new LongbowWorkerThread();
		Thread w = new Thread(Worker.UpdateChannel);
		w.Start();
		
		string inText = string.Empty;
		
		while (loop) {
			inText = Console.ReadLine();
			Console.WriteLine(inText);
		}
		
		
	}
	
}

public class LongbowWorkerThread {
	public int uCursorLeft;
	public int uCursorTop;
	public int nCursorLeft;
	public int nCursorTop;
	public void UpdateChannel(){
		uCursorLeft = Console.CursorLeft;
		uCursorTop = Console.CursorTop;
		while (true){
			Thread.Sleep(5000); 
			nCursorLeft = Console.CursorLeft;
			nCursorTop = Console.CursorTop;
			Console.SetCursorPosition(uCursorLeft, uCursorTop-1);
			Console.WriteLine("Dummy text\r\n");
			Console.SetCursorPosition(nCursorLeft, uCursorTop);
		}
	}
}

public class LongbowToolkit {
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
