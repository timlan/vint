/*
 * Created by SharpDevelop.
 * User: Freddie Nash
 * Date: 02/12/2010
 * Time: 19:27
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
 
#define learningDebugging
 
using System;
using System.Collections.Generic;
using socks = System.Net.Sockets;
using System.Linq;

namespace VInt
{
	class Program
	{
		class TimeParser
		{
			public enum setIssues
			{
				none,
				guessed,
				someUndefined,
			}
			
			public class report
			{
				public TimeSpan resultingTimeSpan;
				
				// variables
				public bool favourLeft;
				public decimal ms = 0, s = 0, m = 0, h = 0, d = 0, w = 0;
				public List<decimal> vals;
				public List<string> seps;
				public List<subSet> sets;
				
				// stuff to set
				public bool empty;
				public bool unhappy;
				public List<string> notes;
				
				public setIssues setIssue;
				public string failure;
				
				public report()
				{
					failure = null;
				}
				
				public report(string failureN)
				{
					failure = failureN;
				}
				
				public void printReport()
				{
					if (failure != null)
					{
						Console.WriteLine("EPIC FAILURE!!!");
						Console.WriteLine(failure);
					}
					else
					{
						if (unhappy)
						{
							Console.WriteLine("Unhappy with the result");
						}
						if (empty)
						{
							Console.WriteLine("Nothing detected or empty input");
						}
						if (setIssue == setIssues.guessed)
						{
							Console.WriteLine("Guessed all subsets, no giveaways detected");
						}
						if (setIssue == setIssues.someUndefined)
						{
							Console.WriteLine("Some sets not defiend by giveaway, guessed favouring " + (favourLeft ? "left" : "right"));
						}
						if (notes.Count > 0)
						{
							Console.WriteLine("Notes:");
							foreach (string ns in notes)
							{
								Console.WriteLine(ns);
							}
						}
					}
				}
			}
			
			public class subSet
			{
				public int index;
				public string name;
				public List<string> giveaways;
				
				public subSet(int iN, string nameN, IEnumerable<string> giveawaysN)
				{
					index = iN;
					name = nameN;
					giveaways = new List<string>();
					giveaways.AddRange(giveawaysN);
				}
			}
			
			public List<subSet> subSets = new List<subSet>();
			public bool favourLeft = true;
			public subSet defaultLowSet;
			public subSet defaultHighSet;
			
			public TimeParser()
			{
				// init
				subSets.Add(new subSet(subSets.Count, "milliseconds", new string[] { "ms", "millisec", "msec", "millisecond", "mils" }));
				subSets.Add(new subSet(subSets.Count, "seconds", new string[] { "s", "sec", "second", "''", "\"" }));
				subSets.Add(new subSet(subSets.Count, "minutes", new string[] { "m", "min", "minute", "'" }));
				subSets.Add(new subSet(subSets.Count, "hours", new string[] { "h", "hour", "hr" }));
				subSets.Add(new subSet(subSets.Count, "days", new string[] { "d", "day" }));
				subSets.Add(new subSet(subSets.Count, "weeks", new string[] { "w", "week", "wk" }));
				
				setDefaultLow("seconds");
			}
			
			public void setDefaultLow(string name)
			{
				// why, why why why?!
				defaultLowSet = subSets.First(ss => ss.name == name);
			}
			
			public void setDefaultHigh(string name)
			{
				defaultHighSet = subSets.First(ss => ss.name == name);
			}
			
			public DateTime parseRelativeDateTime(string line, out report rprt)
			{
				string lowerLine = line.ToLower();
				TimeSpan ts;
				
				if (lowerLine.StartsWith("in") || lowerLine.StartsWith("after"))
				{
					favourLeft = true;
					setDefaultLow("minutes");
					ts = parseTimeSpan(line, out rprt);
					return DateTime.Now.Add(ts);
				}
				else if (lowerLine.EndsWith("ago"))
				{
					favourLeft = true;
					setDefaultLow("minutes");
					ts = parseTimeSpan(line, out rprt);
					return DateTime.Now.Subtract(ts);
				}
				else if (lowerLine.StartsWith("at"))
				{
					favourLeft = false;
					setDefaultHigh("hours");
					ts = parseTimeSpan(line, out rprt);
					return DateTime.Today.Add(ts);
				}
				else
				{
					favourLeft = true;
					setDefaultLow("minutes");
					ts = parseTimeSpan(line, out rprt);
					return DateTime.Now.Add(ts);
				}
			}
			
			public TimeSpan parseTimeSpan(string line, out report rprt)
			{
				decimal ms = 0, s = 0, m = 0, h = 0, d = 0, w = 0;
				
				// tokenise, ish
				List<decimal> vals = new List<decimal>();
				List<string> seps = new List<string>();
				List<subSet> sets = new List<subSet>();
				
				bool unhappy = false;
				bool empty = false;
				List<string> reportNotes = new List<string>();
				bool setsGuessed = false;
				bool someSetsUndefined = false;
				
				int startOfThing = -1;
				bool thingIsVal = false;
				for (int i = 0; i < line.Length; i++)
				{
					if (isNumeric(line[i]))
					{
						if (thingIsVal == false && isOuterNumeric(line[i]))
						{
							if (startOfThing != -1)
							{
								seps.Add(line.Substring(startOfThing, i - startOfThing));
							}
							thingIsVal = true;
							startOfThing = i;
						}
					}
					else
					{
						if (thingIsVal == true)
						{
							if (startOfThing != -1)
							{
								decimal dec;
								if (decimal.TryParse(line.Substring(startOfThing, i - startOfThing), out dec))
									vals.Add(dec);
								else
								{
									rprt = new report("Confused by (numerical?) input : " + line.Substring(startOfThing, i - startOfThing));
									return TimeSpan.Zero;
								}
							}
							thingIsVal = false;
							startOfThing = i;
						}
					}
				}
				
				if (thingIsVal)
				{
					if (startOfThing != -1)
					{
						decimal dec;
						if (decimal.TryParse(line.Substring(startOfThing), out dec))
							vals.Add(dec);
						else
						{
							rprt = new report("Confused by (numerical?) input: " + line.Substring(startOfThing));
							return TimeSpan.Zero;
						}
						seps.Add("");
					}
				}
				else 
				{
					if (startOfThing != -1)
					{
						seps.Add(line.Substring(startOfThing));
					}
				}
				
				if (vals.Count == 0)
				{
					empty = true;
					goto skipToReturn;
				}
				
				// find subsets
				bool addedOne = false;
				for (int i = 0; i < vals.Count; i++)
				{
					sets.Add(detectSubSet(seps[i]));
					if (sets[i] != null)
						addedOne = true;
				}
	
				if (!addedOne) // no subsets detected atall, set the last one to a nice default
				{
					setsGuessed = true;
					if (sets.Count == subSets.Count) // slightly special case
					{
						if (favourLeft)
							sets[sets.Count - 1] = subSets[0];
						else
							sets[0] = subSets[subSets.Count - 1];
					}
					else
					{
						if (favourLeft)
						{
							if (defaultLowSet != null)
								sets[sets.Count - 1] = defaultLowSet;
							else
								sets[0] = defaultHighSet;
						}
						else
						{
							if (defaultLowSet != null)
								sets[0] = defaultHighSet;
							else
								sets[sets.Count - 1] = defaultLowSet;
						}
					}
				}
				
			again:
				bool incomplete = false;
				for (int i = 0; i < vals.Count; i++)
				{
					if (sets[i] == null)
					{
						if (favourLeft)
						{
							if (i > 0 && sets[i - 1] != null)
							{
								int idx = sets[i - 1].index - 1;
								string ifail = clampSubSetIndex(idx);
								if (ifail != null)
								{
									ifail += "\nWhen guessing subset for value " + i + " (" + vals[i] + ") with sep " + seps[i];
									ifail += "\nNo smaller increment suported than milliseconds";
									rprt = new report(ifail);
									return TimeSpan.Zero;
								}
								sets[i] = subSets[idx];
							}
							else if (i < sets.Count - 1 && sets[i + 1] != null)
							{
								int idx = sets[i + 1].index + 1;
								string ifail = clampSubSetIndex(idx);
								if (ifail != null)
								{
									ifail += "\nWhen guessing subset for value " + i + " (" + vals[i] + ") with sep " + seps[i];
									ifail += "\nNo larger increment suported than weeks";
									rprt = new report(ifail);
									return TimeSpan.Zero;
								}
								sets[i] = subSets[idx];
							}
							else
								incomplete = true;	
						}
						else
						{
							if (i < sets.Count - 1 && sets[i + 1] != null)
							{
								int idx = sets[i + 1].index + 1;
								string ifail = clampSubSetIndex(idx);
								if (ifail != null)
								{
									ifail += "\nWhen guessing subset for value " + i + " (" + vals[i] + ") with sep " + seps[i];
									ifail += "\nNo larger increment suported than weeks";
									rprt = new report(ifail);
									return TimeSpan.Zero;
								}
								sets[i] = subSets[idx];
							}
							else if (i > 0 && sets[i - 1] != null)
							{
								int idx = sets[i - 1].index - 1;
								string ifail = clampSubSetIndex(idx);
								if (ifail != null)
								{
									ifail += "\nWhen guessing subset for value " + i + " (" + vals[i] + ") with sep " + seps[i];
									ifail += "\nNo smaller increment suported than milliseconds";
									rprt = new report(ifail);
									return TimeSpan.Zero;
								}
								sets[i] = subSets[idx];
							}
							else
								incomplete = true;
						}
					}
				}
				if (incomplete)
				{
					someSetsUndefined = true;
					goto again;
				}
				
				int lastIdx = subSets.Count;
				for (int i = 0; i < sets.Count; i++)
				{
					int curIdx = sets[i].index;
					if (curIdx > lastIdx)
					{
						unhappy = true;
						reportNotes.Add("Bizzare ordering at index " + i + ", " + sets[i].name + " seems wrong");
					}
					lastIdx = curIdx;
				}
				
				w = 0; d = 0; h = 0; m = 0; s = 0; ms = 0;
				
				// set result decimal
				for (int i = 0; i < vals.Count; i++)
				{
					if (sets[i].name == "weeks")
					{
						if (w != 0)
						{
							unhappy = true;
							reportNotes.Add("More than one weeks values detected at " + i);
						}
						w += vals[i];
					}
					else if (sets[i].name == "days")
					{
						if (d != 0)
						{
							unhappy = true;
							reportNotes.Add("More than one days values detected at " + i);
						}
						d += vals[i];
					}
					else if (sets[i].name == "hours")
					{
						if (h != 0)
						{
							unhappy = true;
							reportNotes.Add("More than one hours values detected at " + i);
						}
						h += vals[i];
					}
					else if (sets[i].name == "minutes")
					{
						if (m != 0)
						{
							unhappy = true;
							reportNotes.Add("More than one minutes values detected at " + i);
						}
						m += vals[i];
					}
					else if (sets[i].name == "seconds")
					{
						if (s != 0)
						{
							unhappy = true;
							reportNotes.Add("More than one seconds values detected at " + i);
						}
						s += vals[i];
					}
					else if (sets[i].name == "milliseconds")
					{
						if (ms != 0)
						{
							unhappy = true;
							reportNotes.Add("More than one milliseconds values detected at " + i);
						}
						ms += vals[i];
					}
				}
				
				d += w * 7;
				
				// tidy results to ints
				h += decimal.Remainder(d, 1) * 24;
				m += decimal.Remainder(h, 1) * 60;
				s += decimal.Remainder(m, 1) * 60;
				ms += decimal.Remainder(s, 1) * 1000;
				ms = decimal.Round(ms);
				
			skipToReturn: // should only happen with VERY special cases (e.g. empty)
				
				// assmble result
				TimeSpan result = new TimeSpan((int)d, (int)h, (int)m, (int)s, (int)ms);
				
				// assemble report
				rprt = new report();
				
				rprt.resultingTimeSpan = result;
				
				rprt.favourLeft = favourLeft;
				
				rprt.w = w;
				rprt.d = d;
				rprt.h = h;
				rprt.m = m;
				rprt.s = s;
				rprt.ms = ms;
				
				rprt.vals = vals;
				rprt.seps = seps;
				rprt.sets = sets;
				
				rprt.empty = empty;
				rprt.unhappy = unhappy;
				rprt.notes = reportNotes;
				
				if (setsGuessed)
					rprt.setIssue = setIssues.guessed;
				else if (someSetsUndefined)
					rprt.setIssue = setIssues.someUndefined;
				else
					rprt.setIssue = setIssues.none;
				
				// return result
				return result;
			}
			
			public subSet detectSubSet(string sep)
			{
				sep = sep.Trim();
				
				subSet best = null;
				int bestGALen = -1;
				
				foreach (subSet ss in subSets)
				{
					foreach (string ga in ss.giveaways)
					{
						if (sep.StartsWith(ga))
						{
							if (best == null || ga.Length > bestGALen)
							{
								best = ss;
								bestGALen = ga.Length;
							}
						}
					}
				}
				
				return best;
			}
			
			// returns failure
			public string clampSubSetIndex(int i)
			{
	//			if (i < 0)
	//				return 0;
	//			if (i > subSets.Count - 1)
	//				return subSets.Count - 1;
	//			return i;
				
				if (i < 0)
					return "Index out of range";
				if (i > subSets.Count - 1)
					return "Index out of range";
				return null;
			}
			
	/*		public static int clampSubSetIndex(int i)
			{
	//			if (i < 0)
	//				return 0;
	//			if (i > subSets.Count - 1)
	//				return subSets.Count - 1;
	//			return i;
				
				if (i < 0)
					throw new Exception("fail");
				if (i > subSets.Count - 1)
					throw new Exception("fail");
				return i;
			}*/
			
			public static bool isNumeric(char c)
			{
				if (c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9')
					return true;
				if (c == '.')
					return true;
				return false;
			}
			
			public static bool isOuterNumeric(char c)
			{
				if (c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9')
					return true;
				return false;
			}
			
			public static bool isInnerNumeric(char c)
			{
				if (c == '.')
					return true;
				return false;
			}
		}
		
		public class alert
		{
			static string encodeStr(string str)
			{
				return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(str));
			}
			
			static string decodeStr(string str)
			{
				return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(str));
			}
			
			public string notice;
			public string person;
			DateTime dt;
			
			public int timeOut = -1;
			
			public bool check()
			{
				if (timeOut == 0)
				{
					timeOut = 100;
					return true;
				}
				else if (timeOut > 0)
				{
					timeOut--;
				}
				else if (System.DateTime.Now > dt)
				{
					timeOut = 100;
					return true;
				}
				return false;
			}
			
			public void stop()
			{
				timeOut = -1;
			}
			
			public alert(string saveString)
			{
				string[] data = saveString.Split(' ');
				notice = decodeStr(data[0]);
				person = decodeStr(data[1]);
				dt = System.DateTime.FromFileTimeUtc(long.Parse(decodeStr(data[2])));
			}
			
			public alert(string personN, DateTime dtN, string noticeN)
			{
				person = personN;
				dt = dtN;
				notice = noticeN;
			}
			
			public alert(DateTime dtN, string noticeN)
			{
				dt = dtN;
				notice = noticeN;
			}
			
//			public alert(string personN, int hourN, int minuteN, string noticeN)
//			{
//				person = personN;
//				hour = hourN;
//				minute = minuteN;
//				notice = noticeN;
//			}
//			
//			public alert(int hourN, int minuteN, string noticeN)
//			{
//				person = "";
//				hour = hourN;
//				minute = minuteN;
//				notice = noticeN;
//			}
			
			public string saveString()
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				
				sb.Append(encodeStr(notice)); sb.Append(' ');
				sb.Append(encodeStr(person)); sb.Append(' ');
				sb.Append(encodeStr(dt.ToFileTimeUtc().ToString()));
				
				return sb.ToString();
			}
		}
		
		static deathFace dFace;
		static VOSParser vp;
		static VNumParser vn;
		static TimeParser tp;
		
		public static List<alert> alerts = new List<alert>();
		public static Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
		public static List<string> noParses = new List<string>() { "lol", "yeah" };
		
		public static void writeOutAlerts()
		{
			System.IO.StreamWriter writer = new System.IO.StreamWriter("alerts.txt");
			foreach (alert a in alerts)
			{
				writer.WriteLine(a.saveString());
			}
			writer.Close();
		}
		
		public static void readAlerts()
		{
			if (System.IO.File.Exists("alerts.txt"))
		    {
				System.IO.StreamReader reader = new System.IO.StreamReader("alerts.txt");
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (line != "")
						alerts.Add(new alert(line));
				}
				reader.Close();
			}
		}
		
		public static void writeOutDictionary()
		{
			System.IO.StreamWriter writer = new System.IO.StreamWriter("dict.txt");
			foreach(string k in dictionary.Keys)
			{
				writer.Write(k + "\n");
				foreach(string s in dictionary[k].Keys)
					writer.Write(s + ":" + dictionary[k][s] + "\n");
			}
			writer.Close();
		}
		
		public static void readDictionary()
		{
			System.IO.StreamReader reader = new System.IO.StreamReader("dict.txt");
			string line, cur = "";
			while ((line = reader.ReadLine()) != null)
			{
				if (!line.Contains(":"))
				{
					cur = line;
					dictionary.Add(cur, new Dictionary<string, string>());
				}
				else
					dictionary[cur].Add(line.Substring(0, line.IndexOf(":")), line.Substring(line.IndexOf(":") + 1));
			}
			reader.Close();
		}
		
		public static void Main(string[] args)
		{
			
			readDictionary();
			
			string msg;
			
			Console.WriteLine("Hello World!");
			
			tp = new TimeParser();
			
			//VOSLearning.init();
			vp = new Program.VOSParser();
			vn = new Program.VNumParser();
			vp.eatDB();
			vp.eatAfixes();
			vp.readInferedCats();
			vp.useEng = true;
			vp.useSVO = true;
			
			dFace = new Program.deathFace();
			dFace.connect();
			dFace.createComs();
			dFace.joinChannel();
			
			while (true)
			{
				msg = dFace.check();
				if (msg != null)
				{
					if (msg.Contains("375"))
					{
						break;
					}
				}
				System.Threading.Thread.Sleep(100);
			}
			
			dFace.joinChannel();
						
			while (true)
			{
				msg = dFace.check();
				if (msg != null && msg.Contains(" JOIN "))
				{
					break;
				}
				System.Threading.Thread.Sleep(200);
			}
			
			dFace.msgNicebot();
			
			mainLoop();
		}
		
		static string dateTimeFormat = "yyyyMMdd_HHmmss";
		static string dateTimeStr()
		{
			return System.DateTime.Now.ToString(dateTimeFormat);
		}
		
		public static long lastPingMin = 0;
		
		public static void mainLoop()
		{
			alerts.Clear();
			readAlerts();
			
			bool displayParseResults = true;
			
			bool running = true;
			string msg, msgNick, end, sender = "";
			string[] data, endData;
			string temp = "Temp not set!!!!";
			
			System.Threading.Thread grabThread = new System.Threading.Thread(new System.Threading.ThreadStart(grab));
			int waiter = 0;
			
			while(running)
			{
				try
				{
				
					// my ping
					if (System.DateTime.Now.Minute != lastPingMin)
					{
						dFace.writeLine("PING AASOL" + dateTimeStr());
						lastPingMin = System.DateTime.Now.Minute;
					}
					
					msg = dFace.check();
					
					if (msg != null)
					{
						msgNick = msg.Split('!')[0].Substring(1);
						msg = msg.Trim();
						data = msg.Split(' ');
						end = msg.Substring(msg.IndexOf(":") + 1);
						if (end.Contains("!"))
							sender = end.Substring(0, end.IndexOf("!"));
						if (end.Contains(":"))
							end = end.Substring(end.IndexOf(":") + 1);
						endData = end.Split(' ');
						if (data[1] == "PRIVMSG")
						{
							Console.WriteLine("'" + end + "'");
							if (end.Contains("FredFace") && dFace.board)
							{
								dFace.sendToBoard("douton 6");
								dFace.sendToBoard("douton 7");
								dFace.sendToBoard("douton 8");
								System.Threading.Thread.Sleep(500);
								dFace.sendToBoard("doutoff 6");
								dFace.sendToBoard("doutoff 7");
								dFace.sendToBoard("doutoff 8");
								System.Threading.Thread.Sleep(500);
								dFace.sendToBoard("douton 6");
								dFace.sendToBoard("douton 7");
								dFace.sendToBoard("douton 8");
								System.Threading.Thread.Sleep(500);
								dFace.sendToBoard("doutoff 6");
								dFace.sendToBoard("doutoff 7");
								dFace.sendToBoard("doutoff 8");
							}
							if (end == "!update")
							{
								try
								{
									System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("../updater.sh");
									psi.UseShellExecute = false;
									psi.RedirectStandardOutput = true;
									psi.RedirectStandardInput = true;
									
									System.Diagnostics.Process updater = System.Diagnostics.Process.Start(psi);
									
									while (!updater.StandardOutput.EndOfStream)
									{
										string updaterStr = updater.StandardOutput.ReadLine();
										
										if (updaterStr == "READY")
										{
											dFace.sendMsg("Updating...");
											
											try {
											dFace.disConnect();
											} catch { } // no faith
											
											updater.StandardInput.WriteLine("THUS");
											
											return; // die horribly
										}
										else
											Console.WriteLine("UPDATER: " + updaterStr);
									}
									
									dFace.sendMsg("Not updating");
								}
								catch (Exception ex)
								{
									dFace.sendMsg("Crash when trying to update: " + ex.Message);
								}
							}
							if (end == "!vpUpdate")
						    {
						    	vp.updateDB();
						    	vp.eatDB();
						    	vp.updateAfixes();
						    	vp.eatAfixes();
						    }
							else if (end == "!vNL")
							{
								vp.useNL = !vp.useNL;
								if (vp.useNL)
									dFace.sendMsg("Using NL");
								else
									dFace.sendMsg("Not Using NL");
							}
							else if (end == "!vSVO")
							{
								vp.useSVO = !vp.useSVO;
								if (vp.useSVO)
									dFace.sendMsg("Using SVO");
								else
									dFace.sendMsg("Not Using SVO");
							}
							else if (end == "!vWordCount")
							{
								dFace.sendMsg(vp.words.Count.ToString() + " Timlan words");
							}
							else if (end.StartsWith("!grabbed"))
							{
								try
								{
									grabThread.Abort();
									dFace.sendToBoard("doutoff 6");
									dFace.sendToBoard("doutoff 7");
									dFace.sendToBoard("doutoff 8");
								}
								catch { }
							}
							else if (end.StartsWith("!alerted"))
							{
								for (int i = alerts.Count - 1; i >= 0; i--)
								{
									if (alerts[i].person == sender && alerts[i].timeOut > -1)
									{
										alerts.RemoveAt(i);
										writeOutAlerts();
									}
								}
							}
							else if (end.StartsWith("!alertme "))
							{
								try
								{
									int cutoff = -1;
									int cutoff2 = -1;
									int t;
									
									end = end.Substring(8);
									
									t = end.LastIndexOf(" in ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 3;
									}
									
									t = end.LastIndexOf(" at ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 3;
									}
									
									t = end.LastIndexOf(" after ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 6;
									}
									
									if (cutoff == -1)
									{
										dFace.sendMsg("What?");
									}
									
									string note = end.Substring(1, cutoff); // trim space
									
									TimeParser.report rprt;
									DateTime dt = tp.parseRelativeDateTime(end.Substring(cutoff + 1), out rprt);
									
									if (rprt.failure == null || DateTime.TryParse(end.Substring(cutoff2 + 1), out dt))
									{
										dFace.sendMsg("You will be alerted at " + dt.ToLongDateString() + " " + dt.ToLongTimeString());
										
										alerts.Add(new alert(sender, dt, note));
										writeOutAlerts();
									}
									else
									{
										dFace.sendMsg("Confused by your datetime sting...");
										dFace.sendMsg(rprt.failure.Replace("\n", "  "));
									}
								}
								catch { }
							}
							else if (end.StartsWith("!alertall "))
							{
								try
								{
									int cutoff = 0;
									int cutoff2 = 0;
									int t;
									
									end = end.Substring(9);
									
									t = end.LastIndexOf(" in ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 3;
									}
									
									t = end.LastIndexOf(" at ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 3;
									}
									
									t = end.LastIndexOf(" after ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 6;
									}
									
									string note = end.Substring(1, cutoff); // trim space
									
									TimeParser.report rprt;
									DateTime dt = tp.parseRelativeDateTime(end.Substring(cutoff + 1), out rprt);
									
									if (rprt.failure == null || DateTime.TryParse(end.Substring(cutoff2 + 1), out dt))
									{
										dFace.sendMsg("Everyone will be alerted at " + dt.ToLongDateString() + " " + dt.ToLongTimeString());
										
										alerts.Add(new alert(dt, note));
										writeOutAlerts();
									}
									else
									{
										dFace.sendMsg("Confused by your datetime sting...");
										dFace.sendMsg(rprt.failure.Replace("\n", "  "));
									}
								}
								catch { }
							}
							else if (end.StartsWith("!alert "))
							{
								try
								{
									// new syntax: !alert <meh> <[in|at|after]> timeclause
									int cutoff = 0;
									int cutoff2 = 0;
									int t;
									
									end = end.Substring(endData[0].Length + endData[1].Length + 2);
									
									t = end.LastIndexOf(" in ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 3;
									}
									
									t = end.LastIndexOf(" at ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 3;
									}
									
									t = end.LastIndexOf(" after ");
									if (t > cutoff)
									{
										cutoff = t;
										cutoff2 = t + 6;
									}
									
									string note = end.Substring(0, cutoff);
									
									TimeParser.report rprt;
									DateTime dt = tp.parseRelativeDateTime(end.Substring(cutoff + 1), out rprt);
									
									if (rprt.failure == null || DateTime.TryParse(end.Substring(cutoff2 + 1), out dt))
									{
										dFace.sendMsg("It will be alerted at " + dt.ToLongDateString() + " " + dt.ToLongTimeString());
										
										alerts.Add(new alert(endData[1], dt, note));
										writeOutAlerts();
									}
									else
									{
										dFace.sendMsg("Confused by your datetime sting...");
										dFace.sendMsg(rprt.failure.Replace("\n", "  "));
									}
								}
								catch { }
							}
							else if (end.StartsWith("!opme"))
							{
								for (int i = alerts.Count - 1; i >= 0; i--)
								{
									if (alerts[i].person == sender && alerts[i].timeOut > -1)
									{
										alerts.RemoveAt(i);
										writeOutAlerts();
									}
								}
							}
							else if (end.StartsWith("!grab"))
							{
								Console.Beep();
								int severity = 0;
								if (end.Length > 6)
									int.TryParse(end.Substring(6), out severity);
								try { grabThread.Abort(); } catch { }
								grabThread = new System.Threading.Thread(new System.Threading.ThreadStart(grab));
								grabThread.Start();
							}
							else if (end.StartsWith("!vwv "))
							{
								dFace.sendMsg(vp.vwv(end.Substring(5)));
							}
							else if (end.StartsWith("!getV "))
							{
								if (end.Length > 6)
								{
									temp = end.Substring(6);
									foreach (VOSParser.word w in vp.words)
									{
										if (w.Eng == temp)
										{
											dFace.sendMsg(w.VOS + " (" + w.catagory + ") : " + w.desc + " | " + w.def);
											if (w.inferedCat != "none")
											{
												dFace.sendMsg("InferedCat: " + w.inferedCat);
											}
										}
									}
								}
							}
							else if (end.StartsWith("!getW "))
							{
								if (end.Length > 6)
								{
									temp = end.Substring(6);
									foreach (VOSParser.word w in vp.words)
									{
										if (w.VOS == temp)
										{
											dFace.sendMsg(w.Eng + ": " + w.VOS + " (" + w.catagory + ") : " + w.desc + " | " + w.def);
											if (w.inferedCat != "none")
											{
												dFace.sendMsg("InferedCat: " + w.inferedCat);
											}
										}
									}
								}
							}
							/*else if (end.StartsWith("!lrn_word "))
							{
								if (end.Length > 10)
								{
									temp = end.Substring(10);
									VOSLearning.vwt[] vwts = VOSLearning.getVwtsOfWord(temp);
									foreach (VOSLearning.vwt v in vwts)
									{
										dFace.sendMsg(v.key + " " + v.word + " " + v.target.ToString() + " " + v.type + " (" + v.depDodge().ToString() + ")");
										System.Threading.Thread.Sleep(1000);
									}
								}
							}
							else if (end.StartsWith("!lrn_type "))
							{
								if (end.Length > 10)
								{
									temp = end.Substring(10);
									VOSLearning.vwt[] vwts = VOSLearning.getVwtsOfType(temp);
									foreach (VOSLearning.vwt v in vwts)
									{
										dFace.sendMsg(v.key + " " + v.word + " " + v.target.ToString() + " " + v.type + " (" + v.depDodge().ToString() + ")");
										System.Threading.Thread.Sleep(1000);
									}
								}
							}
							else if (end.StartsWith("!lrn_type_nouns "))
							{
								if (end.Length > 16)
								{
									temp = end.Substring(16);
									VOSLearning.vwt[] vwts = VOSLearning.getNounsOfType(temp);
									foreach (VOSLearning.vwt v in vwts)
									{
										dFace.sendMsg(v.key + " " + v.word + " " + v.target.ToString() + " " + v.type + " (" + v.depDodge().ToString() + ")");
										System.Threading.Thread.Sleep(1000);
									}
								}
							}*/
							else if (end == "!dpr")
							{
								displayParseResults = !displayParseResults;
								if (displayParseResults)
									dFace.sendMsg("Displaying Parse Results");
								else
									dFace.sendMsg("Not Displaying Parse Results");
							}
							else if (end.StartsWith("!p "))
							{
								dFace.sendMsg(vn.ps.parse(end.Substring(3), false, true));
							}
							else if (end.ToLower().StartsWith("!ev "))
							{
								if ((temp = vn.tryParse(end.Substring(4), false, false)) != "FAILED!")
									dFace.sendMsg(temp);
								else if (vp.tryParse(end.Substring(4), out temp))
									dFace.sendMsg(temp);
							}
							else if (end.ToLower().StartsWith("!vos "))
							{
								if ((temp = vn.tryParse(end.Substring(5), false, false)) != "FAILED!")
									dFace.sendMsg(temp);
								else if (vp.tryParse(end.Substring(5), out temp))
									dFace.sendMsg(temp);
							}
							else if (end.ToLower().StartsWith("!erv "))
							{
								if ((temp = vn.tryParse(end.Substring(5), false, true)) != "FAILED!")
									dFace.sendMsg(temp);
							}
							else if (end.ToLower().StartsWith("!ep "))
							{
								if ((temp = vn.tryParse(end.Substring(4), true, false)) != "FAILED!")
									dFace.sendMsg(temp);
							}
							else if (end.ToLower().StartsWith("!erp "))
							{
								if ((temp = vn.tryParse(end.Substring(5), true, true)) != "FAILED!")
									dFace.sendMsg(temp);
							}
							else if (end.ToLower() == "!printvars")
							{
								foreach (string s in VNumParser.meh.funcs.vars.Keys)
								{
									dFace.sendMsg("== " + s + " " + VNumParser.meh.funcs.vars[s]);
									System.Threading.Thread.Sleep(333);
								}
							}
							else if (!noParses.Contains(end) && vp.tryParse(wordReplace(end, " ei", " /!" + sender + "!/"), out temp))
							{
								if (displayParseResults)
									dFace.sendMsg(temp);
								try {
									if (vp.block.blocks.Count == 3)
									{
										
										VOSParser.VOSBlock verb = vp.block.blocks[0];
										VOSParser.VOSBlock obj = vp.block.blocks[1];
										VOSParser.VOSBlock sbj = vp.block.blocks[2];
										
										if (obj.word == "thing [unknown]")
										{
											if (dictionary.ContainsKey(sbj.word) && dictionary[sbj.word].ContainsKey(verb.word))
												dFace.sendMsg(sbj.word + " " + verb.word + " " + dictionary[sbj.word][verb.word]);
										}
										else
										{
											if (!dictionary.ContainsKey(sbj.word))
												dictionary.Add(sbj.word, new Dictionary<string, string>());
											if (dictionary[sbj.word].ContainsKey(verb.word))
												dictionary[sbj.word][verb.word] += " || " + obj.getString();
											else
												dictionary[sbj.word].Add(verb.word, obj.getString());
											writeOutDictionary();
										}
									}
								}
								catch (Exception ex)
								{
									Console.WriteLine("Err: " + ex.Message);
								}
							}
							else if (end.Contains("{") && end.Contains("}"))
							{
								string result = "";
								bool fail = false;
								
								while (end.Contains("{") && end.Contains("}") && !fail)
								{
									result += end.Substring(0, end.IndexOf("{"));
									Console.Write(end.Substring(end.IndexOf("{") + 1, end.IndexOf("}") - end.IndexOf("{") - 1));
									if ((temp = vn.tryParse(end.Substring(end.IndexOf("{") + 1, end.IndexOf("}") - end.IndexOf("{") - 1), false, false)) != "FAILED!")
										result += temp;
									else if ((temp = vn.tryParse(end.Substring(end.IndexOf("{") + 1, end.IndexOf("}") - end.IndexOf("{") - 1), true, true)) != "FAILED!")
										result += temp;
									else
										fail = true;
									end = end.Substring(end.IndexOf("}") + 1);
								}
								
								if (!fail)
								{
									result += end;
									dFace.sendMsg(result);
								}
							}
							else if ((temp = vn.tryParse(end, false, false)) != "FAILED!")
								dFace.sendMsg("VN Eval: " + temp);
							else if ((temp = vn.tryParse(end, true, true)) != "FAILED!")
								dFace.sendMsg("RP Eval: " + temp);
							else if ((temp = vn.ps.tryParse(end, true, true)) != "FAILED!")
								dFace.sendMsg("VN Pure Eval: " + temp);
							else if ((temp = vn.ps.tryParse(end, false, true)) != "FAILED!")
								dFace.sendMsg("RP Pure Eval: " + temp);
						}
						else if(data[1] == "NOTICE")
						{
							if (end.StartsWith("BOTHELP:"))
							{
								string[] hData = end.Split(':'); 
								if (hData[1] == "IDENTIFY")
								{
									string[] supCmds = hData[2].Split(' ');
									dFace.identifyThreaded(sender, supCmds);
								}
							}
						}
					}
					else
					{
						if (waiter < 200)
							waiter++;
						else
							waiter = waiter = 195;
						if (waiter == 200 && dFace.queryBoard("readdin 1") == "True")
						{
							waiter = 0;
							dFace.sendMsg("Busy!!!!");
							try
							{
								grabThread.Abort();
								dFace.sendToBoard("doutoff 6");
								dFace.sendToBoard("doutoff 7");
								dFace.sendToBoard("doutoff 8");
							}
							catch { }
						}
					}
					
					for (int i = alerts.Count - 1; i >= 0; i--)
					{
						if (alerts[i].check())
						{
							if (alerts[i].person == "")
							{
								dFace.sendMsg(alerts[i].notice);
								alerts.RemoveAt(i);
								writeOutAlerts();
							}
							else
							{
								dFace.sendMsg(alerts[i].person + ": " + alerts[i].notice);
								alerts.RemoveAt(i); // should make a nice repeating system
								writeOutAlerts();
							}
						}
					}
					
					System.Threading.Thread.Sleep(140);
				}
				catch (Exception ex)
				{
					dFace.sendMsg("Not a happy bunny " + ex.Message);
				}
			}
		}
		
		public static void grab()
		{
			while (true)
			{
				dFace.sendToBoard("douton 6");
				System.Threading.Thread.Sleep(500);
				dFace.sendToBoard("douton 7");
				System.Threading.Thread.Sleep(500);
				dFace.sendToBoard("douton 8");
				System.Threading.Thread.Sleep(500);
				dFace.sendToBoard("doutoff 6");
				System.Threading.Thread.Sleep(500);
				dFace.sendToBoard("doutoff 7");
				System.Threading.Thread.Sleep(500);
				dFace.sendToBoard("doutoff 8");
				System.Threading.Thread.Sleep(500);
			}
		}
		
		public static string wordReplace(string str, string oldStr, string newStr)
		{
			string[] data = str.Split(' ');
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] == oldStr)
					data[i] = newStr;
			}
			return string.Join(" ", data);
		}
		
		public class deathFace
		{
			public string nick = "VOSFace";
			public string server = "tim32.org";
			public int port = 6667;
			public string channel = "#tim32";
			public string pswd = "like I'm going to leave this in the code";
			public string nicebot = "NiceBot";
			public string topic = "";
			public bool noPingPong = true;
			public Random rnd = new Random();
			public string proxy = "";
			
			public string id = "VOSFace:FredFace:WhoCares:Timlan/VOS Parsing/Lookup Support Bot";
			public string[] generalHelp;
			public string[] cmdHelp;
			
			public socks.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
			public socks.NetworkStream netStream;
			System.IO.StreamReader reader;
			
			socks.TcpClient boardTcpClient = new System.Net.Sockets.TcpClient();
			public socks.NetworkStream boardNetStream;
			System.IO.StreamReader boardReader;
			public bool board = false;
			
			public List<byte> readBuffer = new List<byte>();
			System.Text.Encoding textEncoding0 = System.Text.Encoding.UTF8;
			System.Text.Encoding textEncoding1 = System.Text.Encoding.GetEncoding("ISO-8859-1");
			System.Text.Encoding curTextEncoding = System.Text.Encoding.UTF8;
			
			public deathFace()
			{
				if (System.IO.File.Exists("name.txt"))
					nick = System.IO.File.ReadAllText("name.txt");
				Console.WriteLine("Nick: " + nick);
			
				if (System.IO.File.Exists("pswd.txt"))
					pswd = System.IO.File.ReadAllText("pswd.txt");
				Console.WriteLine("Pswd: " + pswd);
				
				if (System.IO.File.Exists("server.txt"))
					server = System.IO.File.ReadAllText("server.txt");
				Console.WriteLine("Server: " + server);
				
				if (System.IO.File.Exists("channel.txt"))
					channel = System.IO.File.ReadAllText("channel.txt");
				Console.WriteLine("Channel: " + channel);
				
				if (System.IO.File.Exists("nicebot.txt"))
					nicebot = System.IO.File.ReadAllText("nicebot.txt");
				Console.WriteLine("NiceBot: " + nicebot);
				
				if (System.IO.File.Exists("topic.txt"))
				{
					topic = System.IO.File.ReadAllText("topic.txt");
					Console.WriteLine("Topic: " + topic);
				}
				
				if (System.IO.File.Exists("proxy.txt"))
				{
					proxy = System.IO.File.ReadAllText("proxy.txt");
				}
				
				Console.WriteLine("ID: " + id);
				
				Console.WriteLine("Loading Help Files");
				
				if (System.IO.File.Exists("generalHelp.txt"))
				{
					generalHelp = System.IO.File.ReadAllLines("generalHelp.txt");
				}
				
				if (System.IO.File.Exists("cmdHelp.txt"))
				{
					cmdHelp = System.IO.File.ReadAllLines("cmdHelp.txt");
				}
				
				Console.WriteLine("-- Ready --");
				
				/*try
				{
					// connect to board
					Console.WriteLine("Trying to Connect to BoardSOCK");
					boardTcpClient.Connect("localhost", 1075);
					boardNetStream = boardTcpClient.GetStream();
					boardReader = new System.IO.StreamReader(boardNetStream);
					board = true;
					Console.WriteLine("Connected to BoardSOCK");
				}
				catch
				{
					Console.WriteLine("Attempted Connection to BoardSOCK was a SLUG");
				}*/
			}
			
			public bool buffContains(byte[] buff, byte[] chain)
			{
				for (int i = 0; i <= buff.Length- chain.Length; i++)
				{
					for (int j = 0; j < chain.Length; j++)
					{
						if (buff[i + j] != chain[j])
							goto no;
					}
					return true;
				no:
					continue;
				}
				return false;
			}
			
			public string readLine()
			{
				curTextEncoding = textEncoding0;
				byte nl = 10;
				int b, i = 0;
				// breaks out of loop if we've read in lots of bytes, or if we've reached the end of data
			again:
				while (netStream.DataAvailable && (b = netStream.ReadByte()) != -1)
				{
					if (b == nl)
					{ // if we hit a newline (\n) then we return the contence of the buffer and empty it for re-use
						byte[] bytes = readBuffer.ToArray();
						string temp;
						temp = textEncoding0.GetString(bytes);
						if (buffContains(textEncoding0.GetBytes(temp), new byte[] { 0xEF, 0xBF, 0xBD }))
						{
							curTextEncoding = textEncoding1;
							temp = curTextEncoding.GetString(bytes);
						}
						readBuffer.Clear();
						return temp;
					}
					readBuffer.Add((byte)b);
					i++;
				}
				System.Threading.Thread.Sleep(10);
				goto again;
			}
			
			public void sendToBoard(string msg)
			{
				if (board)
				{
					boardNetStream.Write(curTextEncoding.GetBytes(msg + "\n"), 0, msg.Length + 1);
					Console.WriteLine("Brd: " + msg);
				}
			}
			
			public string queryBoard(string msg)
			{
				if (board)
				{
					boardNetStream.Write(curTextEncoding.GetBytes(msg + "\n"), 0, msg.Length + 1);
					Console.WriteLine("Brd: " + msg);
					return boardReader.ReadLine();
				}
				else
					return "";
			}
			
			public string getRandString(int len)
			{
				string output = "";
				for (int i = 0; i < len; i++)
					output += (char)rnd.Next(0, 255);
				return output;
			}
			
			public void connect()
			{
				tcpClient.Connect(server, port);
				netStream = tcpClient.GetStream();
				while (!netStream.DataAvailable) { }
				reader = new System.IO.StreamReader(netStream, curTextEncoding);
				Console.WriteLine("Connected to " + server);
			}
			
			public void disConnect()
			{
				writeLine("QUIT :tokonahaheek setuh sui");
				tcpClient.Client.Disconnect(false);
				Console.WriteLine("Disconnected!");
			}
			
			public void createComs()
			{
				writeLine("NICK " + nick);
				check();
				writeLine("USER " + nick.ToLower() + " \"tim32.org\" * :vosface");
				check();
			}
			
			public void tryMsg()
			{
				sendMsg(channel, "...Death");
				sendMsg(channel, "s/...Death/Hello Death Face/");
			}
			
			public void joinChannel()
			{
				writeLine("JOIN " + channel);
			}
			
			public void msgNicebot()
			{
				sendMsg(nicebot, pswd);
			}
			
			public string check()
			{
				if (netStream.DataAvailable/* || reader.Peek() != -1*/)
				{
					string output = readLine();//reader.ReadLine();
					if (output != null && output.StartsWith("PING"))
					{
						writeLine("PONG" + output.Substring(4), noPingPong);
						if (!noPingPong)
							Console.WriteLine("Rec: " + output);
						return check();
					}
					else
					{
						Console.WriteLine("Rec: " + output);
						return output;
					}
				}
				return null;
			}
			
			public void sendMsg(string msg)
			{
				if (msg.Contains("\n"))
				{
					string[] strs = msg.Split('\n');
					foreach (string str in strs)
					{
						if (str == "")
							continue;
						writeLine("PRIVMSG " + channel + " :" + str);
						System.Threading.Thread.Sleep(500);
					}
				}
				else
				{
					writeLine("PRIVMSG " + channel + " :" + msg);
				}
			}
			
			public void sendMsg(string targ, string msg)
			{
				if (msg.Contains("\n"))
				{
					string[] strs = msg.Split('\n');
					foreach (string str in strs)
					{
						if (str == "")
							continue;
						writeLine("PRIVMSG " + targ + " :" + str);
						System.Threading.Thread.Sleep(500);
					}
				}
				else
				{
					writeLine("PRIVMSG " + targ + " :" + msg);
				}
			}
			
			public void sendNotice(string targ, string msg)
			{
				if (msg.Contains("\n"))
				{
					string[] strs = msg.Split('\n');
					foreach (string str in strs)
					{
						if (str == "")
							continue;
						writeLine("NOTICE " + targ + " :" + str);
						System.Threading.Thread.Sleep(500);
					}
				}
				else
				{
					writeLine("NOTICE " + targ + " :" + msg);
				}
			}
			
			public void writeLine(string msg)
			{
				byte[] bytes = curTextEncoding.GetBytes(msg + "\n");
				netStream.Write(bytes, 0, bytes.Length);
				Console.WriteLine("Wrt: " + msg);
			}
			
			public void writeLine(string msg, bool noPrint)
			{
				byte[] bytes = curTextEncoding.GetBytes(msg + "\n");
				netStream.Write(bytes, 0, bytes.Length);
				if (!noPrint)
					Console.WriteLine("Wrt: " + msg);
			}
		
			public void identifyThreaded(string sender, string[] supCmds)
			{
				identifyWorkerDel iwd = identifyThreadWorker;
				
				iwd.BeginInvoke(sender, supCmds, null, null);
			}
			
			public delegate void identifyWorkerDel(object sender, object supCmds);
			
			public void identifyThreadWorker(object sender, object supCmds)
			{
				identify((string)sender, (string[])supCmds);
			}
			
			public void identify(string sender, string[] supCmdsArr)
			{
				List<string> supCmds = new List<string>();
				foreach (string cmd in supCmdsArr)
				{
					supCmds.Add(cmd);
				}
				
				// in order of newness
				if (supCmds.Contains("SUPER_ID"))
				{
					sendNotice(sender, "BOTHELP:SUPER_ID:" + id);
				}
				else if (supCmds.Contains("IDENTIFICATION"))
				{
					sendNotice(sender, "BOTHELP:IDENTIFICATION:" + id);
				}
				
				foreach (string cmd in supCmds)
				{
					switch (cmd)
					{
						case "GENERAL":
							if (!supCmds.Contains("G1") && generalHelp != null)
							{
								foreach (string s in generalHelp)
								{
									sendNotice(sender, "BOTHELP:GENERAL:" + s);
									System.Threading.Thread.Sleep(1000);
								}
							}
							break;
						case "COMMAND":
							if (!supCmds.Contains("C1") && cmdHelp != null)
							{
								foreach (string s in cmdHelp)
								{
									sendNotice(sender, "BOTHELP:COMMAND:" + s);
									System.Threading.Thread.Sleep(1000);
								}
							}
							break;
					}
				}
			}
			
		}
		
		public class VOSLearning
		{
			public class rimPhrase
			{
				public string verb;
				public string[] args;
				
				public rimPhrase(string verbN, string[] argsN)
				{
					verb = verbN;
					
					int len = argsN.Length;
					args = new string[len];
					Array.Copy(argsN, args, len);
				}
				
				public static rimPhrase fromVOSBlock(VOSParser.VOSBlock vBlock)
				{
					List<string> argList = new List<string>();
					
					string verb = vBlock.retriveVerb();
					if (verb == null)
						return null;
					
					argList.Add(vBlock.retriveSbj());
					argList.Add(vBlock.retriveObj());
					
					VOSParser.VOSBlock blk = vBlock;
					while (blk.hasParent())
					{
						blk = blk.parent;
						if (blk.blocks != null && blk.blocks.Count == 3 && blk.retriveVerb() == "puc")
						{
							argList.Add(blk.retriveObj());
						}
						else
							break;
					}
					
					foreach (string str in argList)
					{
						if (str == null)
							return null;
					}
					
					return new rimPhrase(verb, argList.ToArray());
				}
				
				public string toString()
				{
					string res = verb;
					foreach (string str in args)
					{
						res += " " + str;
					}
					return res;
				}
			}
			
			/*
			
			/// <summary> Vos Word synType
			/// Describes a small slice of syntax
			/// </summary>
			public class vwt
			{
				/// <summary> vwt key </summary>
				public string key;
				
				/// <summary> Dependancies (keys) - not mutual, more deps here means less dodge </summary>
				public List<string> deps = new List<string>();
				
				/// <summary> the word it concerns </summary>
				public string word;
				/// <summary> syntax target (-1 == word (word is a noun), 0 == sbj, 0 == obj, n (n > 1) == puc[n-2]) </summary>
				public int target;
				/// <summary> target type </summary>
				public string type;
				
				public virtual string toString()
				{
					string res = key + "|";
					
					if (deps.Count > 0)
						 res += deps[0];
					for (int i = 1; i < deps.Count; i++)
					{
						res += ";" + deps[i];
					}
					
					res += "|" + word;
					res += "|" + target;
					res += "|" + type;
					
					return res;
				}
				
				public vwt() { }
				
				public vwt(string keyN, List<string> depsN, string wordN, int targetN, string typeN)
				{
					key = keyN;
					if (depsN != null)
					{
						deps.AddRange(depsN);
					}
					word = wordN;
					target = targetN;
					type = typeN;
				}
				
				public vwt(string dataLine)
				{
					#if learningDebugging
					Console.WriteLine("Loaded Vwt " + dataLine);
					#endif
					
					// name|dep;dep;dep|word|target|type
					string[] data = dataLine.Split('|');
					key = data[0];
					string[] depData = data[1].Split(';');
					
					word = data[2];
					target = int.Parse(data[3]);
					type = data[4];
				}
				
				/// <summary>
				/// 
				/// </summary>
				/// <returns> A Double, 0 = this was pre-alloced, the larger the number, the more dodge </returns></returns>
				public double depDodge()
				{
					if (deps.Count == 0)
						return 0;
					
					double dodge = 1;
					double temp;
					
					foreach (string dep in deps)
					{
						temp = 0;
					
						vwt dv = getVwt(dep);
						temp = dv.depDodge() + 1;
						
						dodge *= temp;
					}
					
					dodge /= deps.Count;
					
					return dodge;
				}
			}
			
			public static int keyLen = 50;
			public static double certaintyLinkModifier = 0.9;
			public static string learningFilePath = "learning.dat";
			public static List<nounType> nounTypes = new List<nounType>();
			public static List<vwt> vwts = new List<vwt>();
			public static Random rnd = new Random();
			
			public static nounType getNounType(string name)
			{
				foreach (nounType n in nounTypes)
				{
					if (n.name == name)
						return n;
				}
				return loadNounType(name);
			}
			
			public static vwt getVwt(string key)
			{
				foreach (vwt v in vwts)
				{
					if (v.key == key)
						return v;
				}
				return loadVwt(key);
			}
			
			// file stuff
			// # == vwt (keys start with #)
			// $ == nounType
			
			// \n is new record
			// | delimitates on record level
			// ; delimitates on dep level
			// , delimitates inside abssub classes
			
			public static void learnFromList(List<rimPhrase> rps)
			{
				foreach (rimPhrase rp in rps)
					learnFrom(rp);
			}
			
			public static void learnFrom(rimPhrase rp)
			{
				#if learningDebugging
				Console.WriteLine(rp.toString());
				#endif
				
				int numArgs = rp.args.Length;
				
				Dictionary<nounType, List<vwt>>[] nts = new Dictionary<nounType, List<vwt>>[numArgs];
				
				for (int i = 0; i < numArgs; i++)
				{
					nts[i] = new Dictionary<nounType, List<vwt>>();
				}
				
				vwt[] verbVwts = getVwtsOfWord(rp.verb);
				
				foreach (vwt v in verbVwts)
				{
					if (v.target < numArgs && v.target >= 0)
					{
						nounType nt = getNounType(v.type);
						if (nts[v.target].ContainsKey(nt))
							nts[v.target][nt].Add(v);
						else
						{
							nts[v.target].Add(nt, new List<vwt>());
							nts[v.target][nt].Add(v);
						}
					}
				}
				
				for (int i = 0; i < numArgs; i++)
				{
					bool matched = false;
					nounType best = null;
					double bestDodge = 0;
					double temp;
					vwt dodgeTester = new vwt();
					
					vwt[] nounVwts = getVwtsOfWord(rp.args[i]);
					dFace.sendMsg("Arg" + i.ToString() + ": " + rp.args[i]);
					foreach (nounType nt in nts[i].Keys)
					{
						foreach (vwt v in nounVwts)
						{
							if (v.target == -1)
							{
								if (v.type == nt.name)
								{
									dFace.sendMsg(nt.name + " - " + nts[i][nt].Count.ToString() + " (match " + v.depDodge().ToString() + ")");
									matched = true;
									goto done;
								}
							}
						}
						dFace.sendMsg(nt.name + " - " + nts[i][nt].Count.ToString());
						
						// for now, just apply the least dodge one
						
						dodgeTester.deps.Clear();
						foreach (vwt dv in nts[i][nt])
						{
							dodgeTester.deps.Add(dv.key);
						}
						temp = dodgeTester.depDodge();
						if (best ==  null || temp < bestDodge)
						{
							best = nt;
							bestDodge = temp;
						}
						
					done:
						System.Threading.Thread.Sleep(500);
					}
					
					if (!matched && best != null)
					{
						vwt tempv = new vwt(generateKey(), null, rp.args[i], -1, best.name);
						foreach (vwt dv in nts[i][best])
						{
							tempv.deps.Add(dv.key);
						}
						vwts.Add(appendVwt(tempv)); // append AFTER so the deps are stored
						dFace.sendMsg("Added: " + tempv.toString());
					}
				}
			}
			
			// assumes it isn't allready in mem (ie. allways add it to vwts list)
			public static vwt loadVwt(string key)
			{
				System.IO.StreamReader reader = new System.IO.StreamReader(learningFilePath);
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (line.Length > key.Length + 2 && line.StartsWith(key))
					{
						vwt res = new vwt(line.Substring(1));
						vwts.Add(res);
						reader.Close();
						return res;
					}
				}
				reader.Close();
				return null;
			}
			
			// assumes it isn't allready in mem (ie. allways add it to nounTypes list)
			public static nounType loadNounType(string name)
			{
				System.IO.StreamReader reader = new System.IO.StreamReader(learningFilePath);
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (line[0] == '$' && line.Length > name.Length + 2 && line.Substring(1, name.Length) == name)
					{ // $ is nountype
						nounType res = new nounType(line.Substring(1));
						nounTypes.Add(res);
						reader.Close();
						return res;
					}
				}
				reader.Close();
				return null;
			}
			
			// retrives an array of all nouns of this type
			public static vwt[] getVwtsOfWord(string word)
			{
				List<vwt> vwtList = new List<vwt>();
				
				System.IO.StreamReader reader = new System.IO.StreamReader(learningFilePath);
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (line[0] == '#' && line.Contains(word))
					{
						vwt res;
						if ((res = vwtLoaded(line.Substring(0, keyLen))) == null)
						{
							res = new vwt(line);
							vwts.Add(res);
						}
						if (res.word == word)
							vwtList.Add(res);
					}
				}
				
				reader.Close();
				return vwtList.ToArray();
			}
			
			// retrives an array of all vwts of this type
			public static vwt[] getVwtsOfType(string type)
			{
				List<vwt> vwtList = new List<vwt>();
				
				System.IO.StreamReader reader = new System.IO.StreamReader(learningFilePath);
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (line[0] == '#' && line.Contains(type))
					{
						vwt res;
						if ((res = vwtLoaded(line.Substring(0, keyLen))) == null)
						{
							res = new vwt(line);
							vwts.Add(res);
						}
						if (res.type == type)
							vwtList.Add(res);
					}
				}
				
				reader.Close();
				return vwtList.ToArray();
			}
			
			// retrives an array of all nouns of this type
			public static vwt[] getNounsOfType(string type)
			{
				List<vwt> vwtList = new List<vwt>();
				
				System.IO.StreamReader reader = new System.IO.StreamReader(learningFilePath);
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (line[0] == '#' && line.Contains(type))
					{
						vwt res;
						if ((res = vwtLoaded(line.Substring(0, keyLen))) == null)
						{
							res = new vwt(line);
							vwts.Add(res);
						}
						if (res.target == -1 && res.type == type)
							vwtList.Add(res);
					}
				}
				
				reader.Close();
				return vwtList.ToArray();
			}
			
			public static vwt vwtLoaded(string key)
			{
				for (int i = vwts.Count - 1; i >= 0; i--)
				{
					if (vwts[i].key == key)
						return vwts[i];
				}
				return null;
			}
			
			public static nounType nounTypeLoaded(string name)
			{
				for (int i = nounTypes.Count - 1; i >= 0; i--)
				{
					if (nounTypes[i].name == name)
						return nounTypes[i];
				}
				return null;
			}
			
			public static bool keyExists(string key)
			{
				System.IO.StreamReader reader = new System.IO.StreamReader(learningFilePath);
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (line.Length > key.Length + 2 && line.Substring(1, key.Length) == key)
					{
						reader.Close();
						return true;
					}
				}
				reader.Close();
				return false;
			}
			
			public static void appendLine(string line)
			{
				System.IO.StreamWriter writer = new System.IO.StreamWriter(learningFilePath, true);
				writer.WriteLine(line);
				writer.Close();
			}
			
			public static string generateKey()
			{
			again:
				string rndKey = "#" + randomKey(keyLen - 1);
				if (keyExists(rndKey))
					goto again;
				return rndKey;
			}
			
			public static string randomKey(int thisKeyLen)
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				for (int i = 0; i < thisKeyLen; i++)
				{
					int ci = rnd.Next(0, 62);
					if (ci < 10)
						ci += 48;
					else if (ci < 36)
						ci += 55;
					else if (ci < 62)
						ci += 61;
					
					sb.Append((char)ci);
				}
				return sb.ToString();
			}
			
			public static vwt appendVwt(vwt v)
			{
				appendLine(v.toString());
				return v;
			}
			
			public static nounType appendNounType(nounType n)
			{
				appendLine("$" + n.toString());
				return n;
			}
			
			// removes anything that contains this key
			public static void remVwt(string key)
			{
				// rem in file
				string fname = "temp" + randomKey(10) + ".dat";
				System.IO.StreamWriter writer = new System.IO.StreamWriter(fname);
				System.IO.StreamReader reader = new System.IO.StreamReader(learningFilePath);
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (!line.Contains(key))
						writer.WriteLine(line);
				}
				reader.Close();
				writer.Close();
				System.IO.File.Replace(fname, learningFilePath, "bu_" + fname);
				
				// rem in mem
				for (int i = vwts.Count - 1; i >= 0; i--)
				{
					if (vwts[i].key == key || vwts[i].deps.Contains(key))
						vwts.RemoveAt(i);
				}
			}
			
			public static void init()
			{
				//debugStuffs();
			}
			
			public static void debugStuffs()
			{
				nounTypes.Add(appendNounType(new nounType("food", "noun")));
				nounTypes.Add(appendNounType(new nounType("colour", "adjective")));
				
				vwts.Add(appendVwt(new vwt(generateKey(), null, "hat~s", -1, "food")));
				vwts.Add(appendVwt(new vwt(generateKey(), null, "cake", -1, "food")));
				vwts.Add(appendVwt(new vwt(generateKey(), null, "felolez", -1, "food")));
				vwts.Add(appendVwt(new vwt(generateKey(), null, "eat", 1, "food")));
			}*/
		}
		
		public class VOSParser
		{

			// common stuffs
			
			public class word
			{
				public string VOS, Eng, catagory, desc, def;
				public string inferedCat = "none";
				public List<string> viableTypes = new List<string>(); // list of VWT Keys
				
				public word(string VOSN, string EngN, string catagoryN, string descN, string defN)
				{
					VOS = VOSN;
					Eng = EngN;
					catagory = catagoryN;
					desc = descN;
					def = defN;
				}
			}
			
			public class afix
			{
				public string longForm; // eg. det
				public string shortForm; // eg. eta
				public string wordCat; // eg. determiner
				public string miniDesc;
				public bool pre; // true means it is a prefix
				public bool isVerbing = false;
				public bool isSupping = false;
				public bool isEvil = false;
				
				public afix(string longFormN, string shortFormN, string wordCatN, string preStr, string miniDescN, string syntaxTypeStr, string evilStr)
				{
					longForm = longFormN;
					shortForm = shortFormN;
					wordCat = wordCatN;
					if (preStr == "1")
						pre = true;
					else
						pre = false;
					if (evilStr == "1")
						isEvil = true;
					else
						isEvil = false;
					miniDesc = miniDescN;
					
					if (syntaxTypeStr == "verboid")
						isVerbing = true;
					else if (syntaxTypeStr == "supplement")
						isSupping = true;
					
					if (isEvil)
						miniDesc = "\x00034" + miniDesc + " (EVIL)\x0F";
				}
			}
			
			public class VOSBlock
			{
				public word vword = null;
				
				public bool container = false;
				
				public string word;
				public string suppliments;
				
				public List<VOSBlock> blocks;
				public VOSBlock parent;
				
				public VOSBlock()
				{
					blocks = new List<VOSBlock>();
					parent = this;
				}
				
				public VOSBlock(VOSParser.VOSBlock myParent)
				{
					blocks = new List<VOSParser.VOSBlock>();
					parent = myParent;
				}
				
				public VOSBlock(string wordN, string supN, VOSBlock myParent)
				{
					word = wordN;
					suppliments = supN;
					parent = myParent;
				}
				
				public VOSBlock(string wordN, string supN, VOSBlock myParent, word vwordN)
				{
					word = wordN;
					suppliments = supN;
					parent = myParent;
					vword = vwordN;
				}
				
				public bool hasParent()
				{
					if (parent == this || parent == null)
						return false;
					return true;
				}
				
				// this tried to get the types of nouns used based on suppliments
//				public void inferCats(int pidx)
//				{
//					if (blocks.Count == 0)
//					{
//						if (vword == null)
//							return;
//						if (pidx == -1)
//						{ // of container
//							return; // single entity in a container
//						}
//					}
//					else
//					{
//						for (int i = 0; i < blocks.Count; i++)
//						{
//							if (container)
//								blocks[i].inferCats(-1);
//							else
//								blocks[i].inferCats(i);
//						}
//					}
//				}
				
				public string getNLStringTop(int indent)
				{
					if (blocks.Count == 0)
						return "";
					string res = blocks[0].getNLString(indent);
					for (int i = 1; i < blocks.Count; i++)
					{
						res += " . " + blocks[i].getNLString(indent);
					}
					return res;
				}
				
				public string getNLString(int indent)
				{
					string output;
					if (word == null)
					{
						output = "";
						for (int i = 0; i < indent; i++)
							output += " ";
						for (int i = 0; i < blocks.Count; i++)
							output += blocks[i].getNLString(indent + 1) + " ";
					}
					else
					{
						if (suppliments == "")
							output = word;
						else
							output = suppliments + " " + word;
					}
					return output + "\n";
				}
				
				public string getNLSVOStringTop(int indent)
				{
					if (blocks.Count == 0)
						return "";
					string res = blocks[0].getNLSVOString(indent);
					for (int i = 1; i < blocks.Count; i++)
					{
						res += " . " + blocks[i].getNLSVOString(indent);
					}
					return res;
				}
				
				public string getNLSVOString(int indent)
				{
					string output;
					if (word == null)
					{
						output = "";
						for (int i = 0; i < indent; i++)
							output += " ";
						output += blocks[blocks.Count - 1].getNLSVOString(indent + 1) + " ";
						for (int i = 0; i < blocks.Count - 1; i++)
							output += blocks[i].getNLSVOString(indent + 1) + " ";
					}
					else
					{
						if (suppliments == "")
							output = word;
						else
							output = suppliments + " " + word;
					}
					return output + "\n";
				}
				
				public string getStringTop()
				{
					if (blocks.Count == 0)
						return "";
					string res = blocks[0].getString();
					for (int i = 1; i < blocks.Count; i++)
					{
						res += " . " + blocks[i].getString();
					}
					return res;
				}
				
				public string getString()
				{
					string output;
					if (word == null)
					{
						output = "( ";
						for (int i = 0; i < blocks.Count; i++)
							output += blocks[i].getString() + " ";
						output = output.Substring(0, output.Length - 1) + " )";
					}
					else
					{
						if (suppliments == "")
							output = word;
						else
							output = suppliments + " " + word;
					}
					return output;
				}
				
				public string getSVOStringTop()
				{
					if (blocks.Count == 0)
						return "";
					string res = blocks[0].getSVOString();
					for (int i = 1; i < blocks.Count; i++)
					{
						res += " . " + blocks[i].getSVOString();
					}
					return res;
				}
				
				public string getSVOString()
				{
					string output;
					if (word == null)
					{
						output = "( ";
						output += blocks[blocks.Count - 1].getSVOString() + " ";
						for (int i = 0; i < blocks.Count - 1; i++)
							output += blocks[i].getSVOString() + " ";
						output = output.Substring(0, output.Length - 1) + " )";
					}
					else
					{
						if (suppliments == "")
							output = word;
						else
							output = suppliments + " " + word;
					}
					return output;
				}
				
				public string retrive()
				{
					if (word == null)
					{
						string rel = retriveVerbRel();
						if (rel == "rela")
							return retriveSbj();
						else if (rel == "relo")
							return retriveObj();
						return null;
					}
					return vword.VOS;
				}
				
				public string retriveRel()
				{
					if (word == null)
						return null;
					if (word.Contains("[relative sbj]") || word.Contains("[rela]"))
						return "rela";
					else if (word.Contains("[relative obj]") || word.Contains("[relo]"))
						return "relo";
					return "";
				}
				
				public string retriveVerbRel()
				{
					return blocks[0].retriveRel();
				}
				
				public string retriveVerb()
				{
					return blocks[0].retrive();
				}
				
				public string retriveObj()
				{
					return blocks[1].retrive();
				}
				
				public string retriveSbj()
				{
					return blocks[blocks.Count - 1].retrive();
				}
				
				public void addRims(List<VOSLearning.rimPhrase> rps)
				{
					if (blocks != null && blocks.Count != 0)
					{
						if (blocks.Count == 3) // it's a verb
						{
							VOSLearning.rimPhrase rp = VOSLearning.rimPhrase.fromVOSBlock(this);
							if (rp != null)
								rps.Add(rp);
						}
					
						for (int i = 0; i < blocks.Count; i++)
						{
							blocks[i].addRims(rps);
						}
					}
					
				}
			}
			
			public List<word> words = new List<word>();
			public string[] pres = new string[] { "soho", "coko", "naha", "peve", "ceve", "wuhi", "rela", "relo", "rila", "rilo", "pece", "toko" };
			public string[] posts = new string[] { "ah", "amo", "eta", "ame", "ami", "eht", "oht", "aht", "elem" };
			
			public afix[] afixes;
				
			public VOSBlock block = new Program.VOSParser.VOSBlock();
			
			public bool useEng = false;
			public bool useSVO = false;
			public bool useNL = false;
			
			public bool learn = true;
			
			public void updateDB()
			{
				System.Net.WebClient webClient = new System.Net.WebClient();
				webClient.Encoding = System.Text.Encoding.UTF8;
				string str = webClient.DownloadString("http://tim32.org/~freddie/VOSLookup/plain.php?VOS=");
				System.IO.StreamWriter writer = new System.IO.StreamWriter("DB.dat", false, System.Text.Encoding.UTF8);
				writer.Write(str);
				writer.Close();
			}
			
			public void updateAfixes()
			{
				System.Net.WebClient webClient = new System.Net.WebClient();
				webClient.Encoding = System.Text.Encoding.UTF8;
				string str = webClient.DownloadString("http://www.tim32.org/~freddie/VOSLookup/fixplain.php?VOS=");
				System.IO.StreamWriter writer = new System.IO.StreamWriter("afixes.dat", false, System.Text.Encoding.UTF8);
				writer.Write(str);
				writer.Close();
			}
			
			public bool eatDB()
			{
				words.Clear();
				System.IO.StreamReader reader = new System.IO.StreamReader("DB.dat", System.Text.Encoding.UTF8);
//				try
//				{
					string r = reader.ReadToEnd();
					r = r.Replace("<br />", "\n");
					string[] data = r.Split('\n');
					string[] d;
					for (int i = 0; i < data.Length; i++)
					{
						string s = data[i];
						int d0 = s.IndexOf("=");
						int d1 = s.IndexOf("(");
						int d2 = s.IndexOf(")");
						int d3 = s.IndexOf("|");
						try
						{
						if (d3 + 2 < s.Length)
							words.Add(new word(s.Substring(0, d0 - 1), s.Substring(d0 + 2, d1 - d0 - 3), s.Substring(d1 + 1, d2 - d1 - 1), s.Substring(d2 + 4, d3 - d2 - 5), s.Substring(d3 + 2)));
						else
							words.Add(new word(s.Substring(0, d0 - 1), s.Substring(d0 + 2, d1 - d0 - 3), s.Substring(d1 + 1, d2 - d1 - 1), s.Substring(d2 + 4, d3 - d2 - 5), ""));
						}
						catch
						{
							
						}
					}
					reader.Close();
					return true;
//				}
//				catch
//				{
//					reader.Close();
//					return false;
//				}
			}
			
			public bool eatAfixes()
			{
				System.IO.StreamReader reader = new System.IO.StreamReader("afixes.dat", System.Text.Encoding.UTF8);
				try
				{
					List<afix> afixList = new List<afix>();
					string r = reader.ReadToEnd();
					r = r.Replace("<br />", "\n");
					string[] lines = r.Split('\n');
					for (int i = 0; i < lines.Length; i++)
					{
						string[] data = lines[i].Split('|');
						try
						{
							afixList.Add(new afix(data[5].Trim(), data[0].Trim(), data[4].Trim(), data[1].Trim(), data[3].Trim(), data[8].Trim(), data[7].Trim()));
						}
						catch
						{
							
						}
					}
					afixes = afixList.ToArray();
					reader.Close();
					return true;
				}
				catch
				{
					reader.Close();
					return false;
				}
			}
			
			public bool tryParse(string str, out string output)
			{
				try { output = parse(str); if(output != null) return true; else return false;}
				catch { output = "FAIL!"; return false; }
			}

			public string parse(string s)
			{
				if (s.Contains("."))
				{
					string combRes = "";
					string[] data = s.Split('.');
					string temp;
					foreach (string sen in data)
					{
						if (sen == "")
							continue;
						if (!tryParse(sen.Trim(), out temp) || temp == null || temp == "FAIL!")
							return null;
						else if (temp != "")
						{
							if (useNL)
								combRes += temp + " .\n";
							else
								combRes += temp + " . ";
						}
					}
					if (combRes.Length < 4)
						return null;
					return combRes.Substring(0, combRes.Length - 3);
				}
				
				string outputF = "VOS Parsing Failed, not sure why....";
				
				try
				{
					
					for (int i = 0; i < s.Length - 2; i++)
					{
						if (s.Substring(i, 2) == "/*")
						{
							while (s.Length > i + 1 && s.Substring(i, 2) != "*/")
								s = s.Remove(i, 1);
							if (s.Substring(i, 2) == "*/")
								s = s.Remove(i, 2);
						}
					}
					
					string sup = "";
					bool isSup = false;
					string temp = "";
					string[] data = s.Split(' ');
					List<string> pre, post;
					string root;
					word rootWord;
					bool isVerb;
					string nextWordInferedCat = "none";
					double valDbl;
					
					VOSBlock topBlock = new VOSBlock();
					VOSBlock curBlock = topBlock;
					VOSBlock tempBlock;
					topBlock.container = true;
					
					for (int i = 0; i < data.Length; i++)
					{
						if (data[i].Contains("/!") && !data[i].Contains("!/"))
						{
							for (int j = i + 1; j < data.Length; j++)
							{
								data[i] += " " + data[j];
								if (data[j].Contains("!/"))
								{
									data[j] = "";
									break;
								}
								data[j] = "";
							}
						}
					}
					
					for (int i = 0; i < data.Length; i++)
					{
						if (data[i].Contains("/:") && !data[i].Contains(":/"))
						{
							for (int j = i + 1; j < data.Length; j++)
							{
								data[i] += " " + data[j];
								if (data[j].Contains(":/"))
								{
									data[j] = "";
									break;
								}
								data[j] = "";
							}
						}
					}
					
					for (int i = 0; i < data.Length; i++)
					{
						if (data[i].Contains("/#") && !data[i].Contains("#/"))
						{
							for (int j = i + 1; j < data.Length; j++)
							{
								data[i] += " " + data[j];
								if (data[j].Contains("#/"))
								{
									data[j] = "";
									break;
								}
								data[j] = "";
							}
						}
					}
					
					for (int i = 0; i < data.Length; i++)
					{
						if (data[i].StartsWith("/\"") && !data[i].EndsWith("\"/"))
						{
							for (int j = i + 1; j < data.Length; j++)
							{
								data[i] += " " + data[j];
								if (data[j].EndsWith("\"/"))
								{
									data[j] = "";
									break;
								}
								data[j] = "";
							}
						}
					}
					
					for (int i = 0; i < data.Length; i++)
					{
							
						if (data[i] == "")
							continue;
						
						temp = "";
						isSup = false;
						isVerb = false;
						pre = getPrefix(data[i]);
						root = data[i].Remove(0, string.Join("", pre.ToArray()).Length);
						post = getPostfix(root);
						if (post.Count > 0)
							root = root.Remove(root.Length - string.Join("", post.ToArray()).Length);
						
						if (data[i].Contains("/!"))
							rootWord = new word(data[i], data[i].Substring(0, data[i].IndexOf("!/")).Substring(data[i].IndexOf("/!") + 2), "noun", "", "");
						else if (data[i].Contains("/:"))
							rootWord = new word(data[i], data[i].Substring(0, data[i].IndexOf(":/")).Substring(data[i].IndexOf("/:") + 2), "noun", "", "");
						else if (data[i].Contains("/#"))
							rootWord = new word(data[i], data[i].Substring(0, data[i].IndexOf("#/")).Substring(data[i].IndexOf("/#") + 2), "noun", "", "");
						else if (data[i].Contains("/\""))
							rootWord = new word(data[i], data[i].Substring(0, data[i].IndexOf("\"/")).Substring(data[i].IndexOf("/\"") + 2), "noun", "", "");
						else if (data[i].Contains("-") && parseNum(root, out valDbl))
						{
							rootWord = new word(root, valDbl.ToString(), "number", "", "");
						}
						else if (root.StartsWith("eli") && root.Length > 3)
						{ // lush element goodness :D
							string meh = "";
							for (int j = 3; j < root.Length; j += 2)
							{
								meh += root.Substring(j, 1);
							}
							if (meh.Length > 1)
								meh = meh.Substring(0, 1).ToUpper() + meh.Substring(1);
							else
								meh = meh.ToUpper();
							rootWord = new word(root, meh, "element", "", "");
						}
						else if (root.StartsWith("komi") && root.Length > 4)
						{ // lush compound goodness :D
							string meh = "";
							for (int j = 4; j < root.Length; j += 0) // the 2 options inc it as they need to
							{
								if (root[j + 1] == 'i')
								{
									meh += root.Substring(j, 1).ToUpper();
									j += 2;
									while (j + 1 < root.Length && root[j + 1] == 'i')
									{
										meh += root.Substring(j, 1).ToLower();
										j += 2;
									}
								}
								else if (root[j + 1] == '-')
								{
									string numMeh = "";
									while (j + 1 < root.Length && root[j + 1] == '-')
									{
										numMeh += root.Substring(j, 2);
										j += 2;
									}
									if (!parseNum(numMeh, out valDbl))
									{
										return "Swale! " + root;
									}
									meh += valDbl.ToString();
								}
							}
							rootWord = new word(root, meh, "element", "", "");
						}
						else if (root.EndsWith("$"))
						{ // lush variable goodness :D
							rootWord = new word(root, root, "variable", "", "");
						}
						else if (data[i] =="puc")
						{
							rootWord = new word("puc", "[puc]", "meh", "", "");
							isVerb = true;
						}
						else
						{
							rootWord = getWord(root);
						}
						
						if (rootWord == null)
							return null;
						else
						{
							foreach (afix a in afixes)
							{
								if (a.pre && a.isVerbing && pre.Contains(a.shortForm))
								{
									isVerb = true;
									rootWord.inferedCat = "verb";
									break;
								}
							}
							if (!isVerb)
							{
								foreach (afix a in afixes)
								{
									if (!a.pre && a.isVerbing && post.Contains(a.shortForm))
									{
										isVerb = true;
										rootWord.inferedCat = "verb";
										break;
									}
								}
							}
							if (isVerb)
							{
								curBlock.blocks.Add(new VOSBlock(curBlock));
								curBlock = curBlock.blocks[curBlock.blocks.Count - 1];
							}
							
							if (useEng)
								temp += rootWord.Eng + " ";
							else
								temp += rootWord.VOS + " ";
						}
						
						foreach (afix a in afixes)
						{
							if ((a.pre && pre.Contains(a.shortForm)) || (!a.pre && post.Contains(a.shortForm)))
							{
								if (a.isSupping)
									isSup = true;
								if (a.wordCat != "")
									rootWord.inferedCat = a.wordCat;
								temp += "[" + a.miniDesc + "] ";
							}
						}
					
						Console.WriteLine(temp);
						
						if (!isVerb && !isSup)
						{
							if (nextWordInferedCat != "none")
							{
								rootWord.inferedCat = nextWordInferedCat;
								nextWordInferedCat = "none";
							}
							else if (rootWord.inferedCat == "none" || rootWord.inferedCat == "noun?")
							{
								rootWord.inferedCat = "noun?";
								if (curBlock.blocks.Count == 1)
								{
									// LONG FORMS
									word longVerb = curBlock.blocks[0].vword;
									foreach (afix a in afixes)
									{
										if (longVerb.VOS == a.longForm)
										{
											if (a.wordCat != "")
												rootWord.inferedCat = a.wordCat;
											break;
										}
									}
								}
							}
						}
						
						if (rootWord != null)
						{
							Console.WriteLine("IC: " + rootWord.inferedCat);
						}
						
						if (isSup)
							sup += temp.Substring(0, temp.Length - 1) + " ";
						else
						{
							if (sup == "")
								curBlock.blocks.Add(new VOSBlock(temp.Substring(0, temp.Length - 1), sup, curBlock, rootWord));
							else
							{
								curBlock.blocks.Add(new VOSBlock(temp.Substring(0, temp.Length - 1), sup.Substring(0, sup.Length - 1), curBlock, rootWord));
								sup = "";
							}
						}
		
						if (!curBlock.container && curBlock.blocks.Count == 3)
						{
							while (!curBlock.container && curBlock.blocks.Count == 3)
							{
								if (curBlock.hasParent() == false)
									return "Y'Swale!";
								curBlock = curBlock.parent;
							}
						}
						
					}
				
					block = topBlock.blocks[0];
					
					try
					{
						if (useSVO)
						{
							if (useNL)
								outputF = topBlock.getNLSVOStringTop(0);
							else
								outputF = topBlock.getSVOStringTop();
						}
						else
						{
							if (useNL)
								outputF = topBlock.getNLStringTop(0);
							else
								outputF = topBlock.getStringTop();
						}
					}
					catch {}
				
					if (learn)
					{
						List<VOSLearning.rimPhrase> rps = new List<VOSLearning.rimPhrase>();
						topBlock.addRims(rps);
						//VOSLearning.learnFromList(rps);
					}
					
					writeOutInferedCats();
					
					return outputF;
					
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message + " " + ex.StackTrace);
					return "FAIL!";
				}				
			}
			
			public void readInferedCats()
			{
				System.IO.StreamReader reader = new System.IO.StreamReader("infcats.dat");
				
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					string[] data = line.Split('|');
					word w = getWord(data[0]);
					if (w != null)
						w.inferedCat = data[1];
				}
				
				reader.Close();
			}
			
			public void writeOutInferedCats()
			{
				System.IO.StreamWriter writer = new System.IO.StreamWriter("infcats.dat");
				
				foreach (word w in words)
				{
					if (w.inferedCat != "none")
					{
						writer.WriteLine(w.VOS + "|" + w.inferedCat);
					}
				}
				
				writer.Close();
			}
			
			public static bool parseNum(string num, out double res)
			{
				return parseNum(num, out res, 16.0);
			}
			
			public static bool parseNum(string num, out double res, double nb)
			{
				if (!num.Contains("-"))
				{
					res = 0.0;
					return false;
				}
				
				try
				{
					int yIndex = -1;
					if ((yIndex = num.IndexOf("y-")) != -1)
				    {
						if (!parseNum(num.Substring(yIndex + 2), out nb))
						{
							res = 0.0;
							return false;
						}
						else
						{
							num = num.Substring(0, yIndex);
						}
				    }
				
					bool negative = false;
					double acc = 0.0;
					double modifier = 1.0;
					
					string[] data;
					int len;
					
					int dn = 1;
					
					if (Math.Abs(nb) > 16)
					{
						dn = (int)Math.Floor(Math.Log(Math.Abs(nb) - 1) / Math.Log(16.0) + 1);
						Console.WriteLine(dn);
						List<string> dataList = new List<string>();
						dataList.AddRange(num.Split('-'));
						for (int i = dataList.Count - 1; i >= 0; i--)
						{
							if (dataList[i] == "w")
							{
								dataList.RemoveAt(i);
								num = num.Remove(i * 2, 2);
								negative = !negative;
							}
							else if (dataList[i] == "z")
							{
								for (int j = 1; j < dn; j++)
								{
									dataList.Insert(i, "z");
									num = num.Insert(i * 2, "z-");
								}
							}
							else if (dataList[i] == "x")
							{
								for (int j = 1; j < dn; j++)
								{
									dataList.Insert(i, "x");
									num = num.Insert(i * 2, "x-");
								}
							}
						}
						data = dataList.ToArray();
					}
					else
					{
						data = num.Split('-');
					}
					len = data.Length - 1;
					Console.WriteLine(num + ": " + nb.ToString());
					
					if (len == 0)
					{
						res = 0.0;
						return false;
					}
					
					for (int i = 0; i < len; i++)
					{
						string s = data[i];
						if (dn > 1)
						{
							if (data[i] == "z")
							{
								modifier = 1.0 / Math.Pow(nb, (len / dn - i / dn - 1));
								acc /= nb;
								goto cont;
							}
							else if (data[i] == "x")
							{
								if (modifier == 1.0)
								{
									modifier = 1.0 / Math.Pow(nb, (len / dn - i / dn - 1));
									acc /= nb;
								}
								double argh = 0;
								if (!parseNum(num.Substring(i * 2 + dn * 2), out argh, nb))
								{
									res = 0;
									return false;
								}
								acc *= Math.Pow(nb, argh);
								goto end;
							}
							s = "";
							for (int j = 0; j < dn; j++)
								s += data[i + j] + "-";
							Console.WriteLine(dn.ToString() + ": " + s + " " + (len / dn - i / dn - 1).ToString());
							double sNum;
							if (!parseNum(s, out sNum))
							{
								res = 0;
								return false;
							}
							acc += Math.Pow(nb, (len / dn - i / dn - 1)) * sNum;
						cont:
							i += dn - 1;
							continue;
						}
					
						if (s == "b")
							continue;
						else if (s == "c")
							acc += Math.Pow(nb, len - i - 1);
						else if (s == "d")
							acc += 2.0 * Math.Pow(nb, len - i - 1);
						else if (s == "f")
							acc += 3.0 * Math.Pow(nb, len - i - 1);
						else if (s == "g")
							acc += 4.0 * Math.Pow(nb, len - i - 1);
						else if (s == "h")
							acc += 5.0 * Math.Pow(nb, len - i - 1);
						else if (s == "j")
							acc += 6.0 * Math.Pow(nb, len - i - 1);
						else if (s == "k")
							acc += 7.0 * Math.Pow(nb, len - i - 1);
						else if (s == "l")
							acc += 8.0 * Math.Pow(nb, len - i - 1);
						else if (s == "m")
							acc += 9.0 * Math.Pow(nb, len - i - 1);
						else if (s == "n")
							acc += 10.0 * Math.Pow(nb, len - i - 1);
						else if (s == "p")
							acc += 11.0 * Math.Pow(nb, len - i - 1);
						else if (s == "r")
							acc += 12.0 * Math.Pow(nb, len - i - 1);
						else if (s == "s")
							acc += 13.0 * Math.Pow(nb, len - i - 1);
						else if (s == "t")
							acc += 14.0 * Math.Pow(nb, len - i - 1);
						else if (s == "v")
							acc += 15.0 * Math.Pow(nb, len - i - 1);
						else if (s == "w")
						{
							negative = !negative;
							acc /= nb;
						}
						else if (s == "x")
						{
							if (modifier == 1.0)
							{
								modifier = 1.0 / Math.Pow(nb, len - i - 1);
								acc /= nb;
							}
							double argh = 0;
							if (!parseNum(num.Substring(i * 2 + 2), out argh, nb))
							{
								res = 0;
								return false;
							}
							acc *= Math.Pow(nb, argh);
							goto end;
						}
						else if (s == "z")
						{
							modifier = 1.0 / Math.Pow(nb, len - i - 1);
							acc /= nb;
						}
						else
						{
							res = 0;
							return false;
						}
					}
				end:
					if (negative)
						acc = -acc;
					acc *= modifier;
					res = acc;
					return true;
					
				}
				catch 
				{
					res = 0;
					return false;
				}
			}
			
			public List<string> getPrefix(string vos)
			{
				string VOS = vos.ToLower();
				List<string> output = new List<string>();
			restart:
				for (int i = 0; i < afixes.Length; i++)
				{
					if (!afixes[i].pre)
						continue;
					if (VOS.StartsWith(afixes[i].shortForm))
					{
						output.Add(afixes[i].shortForm);
						Console.WriteLine("Pre: " + afixes[i].shortForm);
						VOS = VOS.Substring(afixes[i].shortForm.Length);
						goto restart;
					}
				}
				return output;
			}
			
			public List<string> getPostfix(string vos)
			{
				string VOS = vos.ToLower();
				List<string> output = new List<string>();
			restart:
				for (int i = 0; i < afixes.Length; i++)
				{
					if (afixes[i].pre)
						continue;
					if (VOS.EndsWith(afixes[i].shortForm))
					{
						output.Add(afixes[i].shortForm);
						Console.WriteLine("Post: " + afixes[i].shortForm);
						VOS = VOS.Substring(0, VOS.Length - afixes[i].shortForm.Length);
						goto restart;
					}
				}
				return output;
			}
			
			public List<string> getPrefixOld(string vos)
			{
				string VOS = vos.ToLower();
				List<string> output = new List<string>();
			restart:
				for (int i = 0; i < pres.Length; i++)
				{
					if (VOS.StartsWith(pres[i]))
					{
						output.Add(pres[i]);
						VOS = VOS.Substring(pres[i].Length);
						goto restart;
					}
				}
				return output;
			}
			
			public List<string> getPostfixOld(string vos)
			{
				string VOS = vos.ToLower();
				List<string> output = new List<string>();
			restart:
				for (int i = 0; i < posts.Length; i++)
				{
					if (VOS.EndsWith(posts[i]))
					{
						output.Add(posts[i]);
						VOS = VOS.Substring(0, VOS.Length - posts[i].Length);
						goto restart;
					}
				}
				return output;
			}
			
			public afix getAfix(string shortForm)
			{
				for (int i = 0; i < afixes.Length; i++)
				{
					if (afixes[i].shortForm == shortForm)
						return afixes[i];
				}
				return null;
			}
			
			public word getWord(string VOS)
			{
				VOS = VOS.ToLower();
				for (int i = 0; i < words.Count; i++)
				{
					if (words[i].VOS == VOS)
						return words[i];
				}
				return null;
			}
			
			string[] alphabet = new string[]
			{
				"a",
				"b",
				"c",
				"d",
				"e",
				"f",
				"g",
				"h",
				"i",
				"j",
				"k",
				"l",
				"m",
				"n",
				"o",
				"p",
				"q",
				"r",
				"s",
				"t",
				"u",
				"v",
				"w",
				"x",
				"y",
				"z",
				"t~s",
				"t~c",
				"d~j",
				"d~z"
			};
			string[] alphabetTypes = new string[]
			{
				"V",
				"C",
				"F",
				"C",
				"V",
				"F",
				"C",
				"F",
				"V",
				"F",
				"C",
				"C",
				"N",
				"N",
				"V",
				"C",
				"V",
				"C",
				"F",
				"C",
				"V",
				"F",
				"C",
				"F",
				"C",
				"F",
				"A",
				"A",
				"A",
				"A"
			};
			
			public class VWVException : Exception
			{
				public VWVException(string msg) : base(msg)
				{
				}
			}
			
			public string getLetterType(string letter)
			{
				int idx = Array.IndexOf(alphabet, letter);
				if (idx < 0)
					throw new VWVException("Unrecognised letter: " + letter);
				return alphabetTypes[idx];
			}
		
			public string nextLetter(string w, ref int i)
			{
				string res = w.Substring(i, 1);
				
				if (i + 2 < w.Length)
				{
					if (w[i + 1] == '~')
					{
						res = w.Substring(i, 3);
						i += 2;
					}
				}
				
				i++;
				return res;
			}
			
			public string vwv(string wv)
			{
				try
				{
					wv = wv.ToLower();
					
					if (wv.Length < 2)
						return "2 letters or more please";
					
					int i = 0;
					string prev = nextLetter(wv, ref i);
					string prevType = getLetterType(prev);
					for (; i < wv.Length;)
					{
						string cur = nextLetter(wv, ref i);
						string curType = getLetterType(cur);
						
						if (prevType != "V" && curType != "V")
						{
							if ((prevType == "F" || prevType == "N") && curType == "C")
							{
							}
							else
								return "Illegal pair: " + prevType + curType + " (" + prev + cur + ")";
						}
						
						prev = cur;
						prevType = curType;
					}
					
					
					foreach (word w in words)
					{
						if (wv == w.VOS)
							return "Word match: " + w.VOS + " - " + w.Eng;
					}
					
					
					foreach (afix a in afixes)
					{
						if (a.pre && (wv.StartsWith(a.shortForm) || a.shortForm.StartsWith(wv)))
							return "Prefix match: " + a.shortForm;
						if (!a.pre && (wv.EndsWith(a.shortForm) || a.shortForm.EndsWith(wv)))
							return "Postfix match: " + a.shortForm;
					}
					
					
					return "Looks ok";
				}
				catch (VWVException vwvex)
				{
					return vwvex.Message;
				}
			}
		}
		
		public class VNumParser
		{
			
			double ans = 1.0;
			public pureStacker ps = new Program.pureStacker();
			
			public class item
			{
				public string verb, name;
				public item obj, sbj, parent;
				public object value;
				
				public object getObject()
				{
					if (verb == "" || verb == null)
						return value;
					System.Reflection.MethodInfo mi = (meh.funcs).GetType().GetMethod(verb);
					return (double)mi.Invoke(meh.funcs, new object[] {sbj.getObject(), obj.getObject()});
				}
				
				public object getPolishObject()
				{
					if (verb == "" || verb == null)
						return value;
					System.Reflection.MethodInfo mi = (meh.funcs).GetType().GetMethod(verb);
					return (double)mi.Invoke(meh.funcs, new object[] {obj.getPolishObject(), sbj.getPolishObject()});
				}
				
				public item() { }
				
				public item(item parentN)
				{
					parent = parentN;
				}
				
				public item(double val)
				{
					value = (object)val;
				}
				
				public item(string val)
				{
					value = (object)val;
				}
			}

			public string tryParse(string input, bool polish, bool reverse)
			{
				string output = "FAILED!";
				try { output = parse(input, polish, reverse); } catch { }
				return output;
			}

			public string parse(string input, bool polish, bool reverse)
			{
				
				if (input == "info")
					return "This is the best VOS/OSV/VSO/SOV parser that Tim32 uses - it has the largest number of functions supported and uses a full block parsing mechanism just because. Thats right, its an original VNum. There is a pure parser too... Because they are more flexyface";
								
				if (input.Length > 5 && input[0] == '_' && input[1] == 'p' && input[5] == '_')
				{
					switch (input.Substring(2, 3).ToLower())
			        {
						case "vos":
							return ps.parse(input.Substring(7), true, true);
						case "osv":
							return ps.parse(input.Substring(7), false, false);
						case "vso":
							return ps.parse(input.Substring(7), true, false);
						case "sov":
							return ps.parse(input.Substring(7), false, true);
			        }
					input = input.Substring(6);
				}
				
				if (input.Length > 4 && input[0] == '_' && input[4] == '_')
				{
					switch (input.Substring(1, 3).ToLower())
			        {
						case "vos":
							polish = false;
							reverse = false;
							break;
						case "osv":
							polish = false;
							reverse = true;
							break;
						case "vso":
							polish = true;
							reverse = false;
							break;
						case "sov":
							polish = true;
							reverse = true;
							break;
			        }
					input = input.Substring(6);
				}
			
				// just imagine this isn't here, and it'll be alright... :)
				if (reverse)
					polish = !polish;
				
				bool res;
				double d;
				item top = new item();
				item it = top;
				
				// * -> mul is done separatly due to variables
				// sub has '- ' so that negative numbers can be used (ie. -8)
				input = input.Replace("(", "").Replace(")", "").Replace("+", "add").Replace(" - ", " sub ").Replace("/", "div").Replace("==", "eq").Replace("=", "set").Replace(">", "gt").Replace(">=", "ge").Replace("<", "lt").Replace("<=", "le").Replace("%", "mod").Replace("ans", ans.ToString()).Replace("epsilon", double.Epsilon.ToString()).Replace("pi", Math.PI.ToString());
				if (input.EndsWith(" -"))
					input = input.Substring(0, input.Length - 1) + "sub";
				else if (input.StartsWith("- "))
					input = "sub" + input.Substring(1);
				string[] data = input.Split(' ');
				if (reverse)
					Array.Reverse(data);
				
				for (int i = 0; i < data.Length; i++)
				{
					
					if (data[i] == "")
						continue;
					
					foreach (string s in meh.funcs.vars.Keys)
					{
						if (data[i] == "*" + s)
							data[i] = meh.funcs.vars[s].ToString();
					}
					if (data[i] == "*")
						data[i] = "mul";
					else if (data[i] == "rnd")
					{
						Random rnd = new Random();
						data[i] = rnd.NextDouble().ToString();
					}
				
					if (data[i][0] == '&')
					{
						if (it.obj == null)
						{
							it.obj = new item(data[i].Substring(1));
						}
						else
						{
							while (it.sbj != null)
								it = it.parent;
							it.sbj = new item(data[i].Substring(1));
							it = it.parent;
						}
						continue;
					}
					res = Double.TryParse(data[i], out d);
					if (!res)
					{
						res = VOSParser.parseNum(data[i], out d);
					}
					if (res)
					{
						if (it.obj == null)
						{
							it.obj = new item(d);
						}
						else
						{
							while (it.sbj != null)
								it = it.parent;
							it.sbj = new item(d);
							it = it.parent;
						}
					}
					else
					{
						if (it.verb == "" || it.verb == null)
						{
							it.verb = data[i];
						}
						else if (it.obj == null)
						{
							it.obj = new item(it);
							it = it.obj;
							it.verb = data[i];
						}
						else
						{
							while (it.sbj != null)
								it = it.parent;
							it.sbj = new item(it);
							it = it.sbj;
							it.verb = data[i];
						}
					}
				}
				
				if (polish)
					ans = (double)top.getPolishObject();
				else
					ans = (double)top.getObject();
				return ans.ToString();
				
			}
			
			public static class meh
			{
				public static funcsClass funcs = new funcsClass();
			}
	
			public class funcsClass
			{
			
				public Dictionary<string, double> vars = new Dictionary<string, double>();
				
				public double set(string a, double b)
				{
					if (vars.ContainsKey(a))
						vars[a] = b;
					else
						vars.Add(a, b);
					return b;
				}
				
				// normal thingys
				
				public double add(double a, double b)
				{
					return a + b;
				}
				public double sub(double a, double b)
				{
					return a - b;
				}
				public double mul(double a, double b)
				{
					return a * b;
				}
				public double div(double a, double b)
				{
					return a / b;
				}
				public double pow(double a, double b)
				{
					return Math.Pow(a, b);
				}
				public double mod(double a, double b)
				{
					int n = (int)a;
					while (n >= b)
						n -= (int)b;
					return n;
				}
				public double nod(double a, double b)
				{
					return a * Math.Sqrt(b);
				}
				public double root(double a, double b)
				{
					return Math.Pow(a, 1.0 / b);
				}
				public double eq(double a, double b)
				{
					if (a == b)
						return 1.0;
					else
						return 0.0;
				}
				public double gt(double a, double b)
				{
					if (a > b)
						return 1.0;
					else
						return 0.0;
				}
				public double ge(double a, double b)
				{
					if (a >= b)
						return 1.0;
					else
						return 0.0;
				}
				public double lt(double a, double b)
				{
					if (a < b)
						return 1.0;
					else
						return 0.0;
				}
				public double le(double a, double b)
				{
					if (a <= b)
						return 1.0;
					else
						return 0.0;
				}
				
				// fun thingys
				
				public double pythag(double a, double b)
				{
					return Math.Sqrt(a * a + b * b);
				}
				
				// single thingys
				
				public double sign(double a, double b)
				{
					return Math.Sign(a);
				}
				public double abs(double a, double b)
				{
					return Math.Abs(a);
				}
				public double sqrt(double a, double b)
				{
					return Math.Sqrt(a);
				}
				public double log(double a, double b)
				{
					return Math.Log(a, b);
				}
				
				public double degRad(double a, double b)
				{
					return a / 180.0 * Math.PI;
				}
				
				public double radDeg(double a, double b)
				{
					return a / Math.PI * 180.0;
				}
				
				public double sinD(double a, double b)
				{
					return Math.Sin(a / 180 * Math.PI);
				}
				public double cosD(double a, double b)
				{
					return Math.Cos(a / 180 * Math.PI);
				}
				public double tanD(double a, double b)
				{
					return Math.Tan(a / 180 * Math.PI);
				}
				public double sin(double a, double b)
				{
					return Math.Sin(a);
				}
				public double cos(double a, double b)
				{
					return Math.Cos(a);
				}
				public double tan(double a, double b)
				{
					return Math.Tan(a);
				}
				
			}

		}
			
		public class pureStacker
		{
			
			public Dictionary<string, object> variables = new Dictionary<string, object>();
			public List<object> stack = new List<object>();
			public double ans = 0.0;
			public int req = 0;
			public bool reverse;
			
			public string tryParse(string input, bool polish, bool reverse)
			{
				string output = "FAILED!";
				try { output = parse(input, polish, reverse); } catch { }
				return output;
			}
				
			public string parse(string input, bool rtl, bool rereverse)
			{
				
				reverse = rereverse;
				
				input = input.Replace("(", "").Replace(")", "").Replace("+", "add").Replace(" - ", " sub ").Replace("*", "mul").Replace("/", "div").Replace("==", "eq").Replace("=", "set").Replace(">", "gt").Replace(">=", "ge").Replace("<", "lt").Replace("<=", "le").Replace("%", "mod").Replace("ans", ans.ToString()).Replace("epsilon", double.Epsilon.ToString()).Replace("pi", Math.PI.ToString());
				if (input.EndsWith(" -"))
					input = input.Substring(0, input.Length - 1) + "sub";
				else if (input.StartsWith("- "))
					input = "sub" + input.Substring(1);
				
				stack.Clear();
				bool res;
				double d;
				string[] data = input.Split(' ');
	
				if (rtl)
					Array.Reverse(data);
				
				foreach (string s in data)
				{
					
					if (s == "")
						continue;
					
					if (s.StartsWith("$"))
					{
						stack.Add((object)s);
						continue;
					}
					res = Double.TryParse(s, out d);
					if (!res)
					{
						res = VOSParser.parseNum(s, out d);
					}
					if (res)
					{
						stack.Add((object)d);
					}
					else 
					{
						System.Reflection.MethodInfo mi = (this).GetType().GetMethod(s);
						stack.Add((object)mi.Invoke(this, new object[] {}));
					}
				}
				
				ans = (double)stack[0];
				
				return ans.ToString();
			}
			
			object popO()
			{
				object obj;
				if (reverse)
				{
					obj = stack[stack.Count - req];
					stack.RemoveAt(stack.Count - req);
					req--;
				}
				else
				{
					obj = stack[stack.Count - 1];
					stack.RemoveAt(stack.Count - 1);
				}
				return obj;
			}
			
			double popD()
			{
				object obj = popO();
				if (obj.GetType() == ("").GetType())
					return (double)variables[((string)obj).Substring(1)];
				else
					return (double)obj;
			}
			
			string popV()
			{
				return (string)popO();
			}
			
			public double add()
			{
				req = 2;
				return popD() + popD();
			}
			
			public double sub()
			{
				req = 2;
				return popD() - popD();
			}
			
			public double mul()
			{
				req = 2;
				return popD() * popD();
			}
			
			public double div()
			{
				req = 2;
				return popD() / popD();
			}
			
			public double sqrt()
			{
				req = 1;
				return Math.Sqrt(popD());
			}
			
			public double sign()
			{
				req = 1;
				return Math.Sign(popD());
			}
			public double abs()
			{
				req = 1;
				return Math.Abs(popD());
			}
			
			public double sinD()
			{
				req = 1;
				return Math.Sin(popD() / 180 * Math.PI);
			}
			public double cosD()
			{
				req = 1;
				return Math.Cos(popD() / 180 * Math.PI);
			}
			public double tanD()
			{
				req = 1;
				return Math.Tan(popD() / 180 * Math.PI);
			}
			public double sin()
			{
				req = 1;
				return Math.Sin(popD());
			}
			public double cos()
			{
				req = 1;
				return Math.Cos(popD());
			}
			public double tan()
			{
				req = 1;
				return Math.Tan(popD());
			}
			
			public object set()
			{
				req = 2;
				object obj = pop();
				variables[popV()] = obj;
				return obj;
			}
			
			public object pop()
			{
				req = 1;
				popO();
				req = 1;
				return popO();
			}
		}

	}
}
