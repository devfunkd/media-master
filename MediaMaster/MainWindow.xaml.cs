using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;

namespace MediaMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// TODO: Clean up / Refactor (Move to ViewModel)
    public partial class MainWindow : Window
    {
        string SelectedFolder = null;
        string IMDBAPI = "http://www.omdbapi.com/?i={0}&plot=short&r=json";
        string FarooSearchLink = "http://www.faroo.com/api?q={0}&start=1&length=10&l=en&src=web&f=json";
        public MainWindow()
        {
            InitializeComponent();
            FilesFoundLabel.Content = "Waiting ....";
            FilesRenamedSuccessLabel.Content = "0 Files Renamed";
            FilesNotRenamedLabel.Content = "0 Files Not Changed";
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowDialog();
            if (dialog?.SelectedPath?.Trim() == "")
                return;
            SelectedFolder = dialog.SelectedPath;
            StartProcessButton.IsEnabled = true;
            FilesFoundLabel.Content = System.IO.Directory.GetFiles(SelectedFolder, "*.*", System.IO.SearchOption.AllDirectories).Length + " Files Found ";
            FilesRenamedSuccessLabel.Content = "0 Files Renamed";
            FilesNotRenamedLabel.Content = "0 Files Not Changed";
        }

        string Episode = "";
        string Season = "";
        private void StartProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFolder == null || SelectedFolder.Trim() == "")
                return;
            List<string> ListOfFiles = System.IO.Directory.GetFiles(SelectedFolder, "*.*", System.IO.SearchOption.AllDirectories).ToList<string>();
            List<string> FilesNamesNotFound = new List<string>();
            List<string> FilesNamesFound = new List<string>();
            List<string> FilesNamesFoundNEW = new List<string>();
            double TotalFiles = ListOfFiles.Count;
            double Done = 0;
            StatusProgressBar.Value = 1;
            StartProcessButton.IsEnabled = false;
            SelectFolderButton.IsEnabled = false;
            new Thread((ThreadStart)delegate
            {
                ListOfFiles.ForEach(X =>
                {
                    #region If is Video file then process
                    if (isVideoFile(X))
                    {
                        var FileName = X.Split('\\')[X.Split('\\').Length - 1];
                        string ActualFileName = "";
                        string Year = "";
                        Episode = "";
                        Season = "";
                        FileName = FileName.Replace(".", " ").Replace("-", " ");
                        #region Create Search String For Google Search API
                        string SearchString = "";
                        bool KnowEpisode = false;
                        foreach (string Token in FileName.Split(' '))
                        {
                            int Num = 0;
                            if (!KnowEpisode && ISEPISODE(Token))
                            {
                                KnowEpisode = true;
                            }
                            else if (int.TryParse(Token, out Num))
                            {
                                int NumOfDigits = (int)Math.Floor(Math.Log10(Num) + 1);
                                if (NumOfDigits == 4)
                                    Year = Num.ToString();
                                continue;
                            }
                            else if (Token.Trim() == "")
                                continue;
                            else if (ContainsPunctuation(Token)
                            || ContainsNumber(Token))
                                continue;
                            else if (Token.ToLower().Contains("hdtv")
                            || Token.ToLower().Contains("xvid")
                            || isVideoFile(Token)
                            || Token.ToLower().Contains("eng")
                            || Token.ToLower().Contains("rus")
                            )
                                continue;
                            else
                                SearchString += Token + " ";
                        }
                        SearchString += Year;
                        #endregion
                        #region GET IMDB Details
                        IMDBDETAILS TDetails = null;
                        string OrignalSearchString = SearchString;
                        do
                        {
                            TDetails = GetJasonData<IMDBDETAILS>(string.Format("http://www.omdbapi.com/?t={0}&y={1}&plot=short&r=json", SearchString, Year));
                            var TT = SearchString.Split(' ').ToList();
                            if (TT.Count > 0)
                                TT.RemoveAt(TT.Count - 1);
                            SearchString = string.Join(" ", TT);
                        } while (TDetails?.Title == null && SearchString.Trim() != "");
                        if (TDetails?.Title == null)
                        {
                            FilesNamesNotFound.Add(X.Replace(SelectedFolder, ""));
                            return;
                        }
                        else
                        {
                            Season = ProcessNumber(Season);
                            Episode = ProcessNumber(Episode);
                            FilesNamesFound.Add(X.Replace(SelectedFolder, ""));
                            ActualFileName = TDetails.Title + " (" + TDetails.Year.Replace("â€“", "") + ")";
                            if (TDetails.Type != "movie")
                                ActualFileName += " S" + Season + " E" + Episode + "";
                            ActualFileName = ActualFileName.Replace("\\", "").Replace(">", "").
                            Replace("/", "").Replace("*", "").Replace("\"", "").Replace("|", "").
                            Replace(":", "").Replace("?", "").Replace("<", "");
                            FilesNamesFoundNEW.Add(ActualFileName);
                        }
                        #endregion
                    }
                    else return;
                    #endregion
                    Done++;
                    Dispatcher.Invoke((Action)delegate
                    {
                        StatusProgressBar.Value = (Done / TotalFiles * (double)100);
                        FilesRenamedSuccessLabel.Content = "Parsing Names";
                        FilesNotRenamedLabel.Content = Done + " Done out of " + TotalFiles;
                    });
                });
                Dispatcher.Invoke((Action)delegate
                {
                    StatusProgressBar.Value = 0;
                    FilesRenamedSuccessLabel.Content = "Fixing Duplicates";
                    FilesNotRenamedLabel.Content = "Almost Done";
                });
                #region Finding and Removing Duplicated
                //Finding Duplicated
                Hashtable Duplicates = new Hashtable(1000);
                FilesNamesFoundNEW.ForEach(X =>
                {
                    var All = FilesNamesFoundNEW.FindAll(XX => XX == X);
                    if (All.Count == 0 || All.Count == 1) return;
                    else
                    {
                        try { Duplicates.Add(FilesNamesFoundNEW.IndexOf(X), All); } catch { }
                    }
                });
                //Removing Duplicated
                foreach (List<string> Entries in Duplicates.Values)
                    foreach (var Entry in Entries)
                    {
                        int index = FilesNamesFoundNEW.FindIndex(X => X == Entry);
                        if (index != -1)
                        {
                            FilesNamesFoundNEW.RemoveAt(index);
                            FilesNamesNotFound.Add(FilesNamesFound[index]);
                            FilesNamesFound.RemoveAt(index);
                        }
                    }
                #endregion
                Dispatcher.Invoke((Action)delegate
                {
                    StatusProgressBar.Value = 0;
                    FilesRenamedSuccessLabel.Content = "Renaming";
                    FilesNotRenamedLabel.Content = "Almost Done";
                });
                TotalFiles = FilesNamesFoundNEW.Count;
                Done = 0;
                #region Renaming 
                //Renaming
                for (int I = 0; I < FilesNamesFound.Count; I++)
                {
                    string Directory = System.IO.Path.GetDirectoryName(FilesNamesFound[I]);
                    string PathToDir = SelectedFolder + Directory;
                    if (Directory?.Contains("\\") == true)
                        Directory = Directory.Split('\\')[Directory.Split('\\').Length - 1];
                    if (Directory?.StartsWith("\\") == true) Directory = Directory.Remove(0, 1);
                    if (Directory?.Trim() == "")
                    {
                        System.IO.File.Move(SelectedFolder + FilesNamesFound[I], SelectedFolder + "\\" + FilesNamesFoundNEW[I] + "." + FilesNamesFound[I].Split('.')[FilesNamesFound[I].Split('.').Length - 1]);
                    }
                    else
                    {
                        string TempPath = PathToDir.Remove(PathToDir.Length - Directory.Length);
                        string DirName = PathToDir.Replace(TempPath, "");
                        if (SelectedFolder + FilesNamesFound[I] != TempPath + DirName + "\\" + FilesNamesFoundNEW[I] + "." + FilesNamesFound[I].Split('.')[FilesNamesFound[I].Split('.').Length - 1])
                        {
                            try
                            {
                                System.IO.File.Move(SelectedFolder + FilesNamesFound[I], TempPath + DirName + "\\" + FilesNamesFoundNEW[I] + "." + FilesNamesFound[I].Split('.')[FilesNamesFound[I].Split('.').Length - 1]);
                            }
                            catch { }
                        }
                        string FolderName = FilesNamesFoundNEW[I];
                        FolderName = FolderName.Replace("S0", "S#").Replace("S1", "S#")
                        .Replace("S2", "S#").Replace("S3", "S#").Replace("S4", "S#")
                        .Replace("S5", "S#").Replace("S6", "S#").Replace("S7", "S#")
                        .Replace("S8", "S#").Replace("S9", "S#");
                        if (FolderName.Contains(" S#"))
                            FolderName = FolderName.Substring(0, FolderName.IndexOf(" S#"));
                        if (PathToDir == TempPath + FolderName)
                            continue;
                        if (!System.IO.Directory.Exists(TempPath + FolderName))
                            System.IO.Directory.Move(PathToDir, TempPath + FolderName);
                        else
                        {
                            List<String> MyMusicFiles = System.IO.Directory.
                            GetFiles(PathToDir, "*.*", System.IO.SearchOption.AllDirectories).ToList();
                            foreach (string file in MyMusicFiles)
                            {
                                DirectoryInfo dirInfo = new DirectoryInfo(TempPath + FolderName);
                                FileInfo mFile = new FileInfo(file);
                                // to remove name collusion
                                if (new FileInfo(dirInfo + "\\" + mFile.Name).Exists == false)
                                    mFile.MoveTo(dirInfo + "\\" + mFile.Name);
                            }
                        }
                        foreach (var Element in FilesNamesFound.FindAll(X => X.Contains(DirName)))
                        {
                            int Index = FilesNamesFound.IndexOf(Element);
                            if (Index != -1)
                                FilesNamesFound[Index] = FilesNamesFound[Index].Replace(DirName, FolderName);
                        }
                    }
                    Dispatcher.Invoke((Action)delegate
                    {
                        StatusProgressBar.Value = (Done / TotalFiles * (double)100);
                        FilesRenamedSuccessLabel.Content = "Renaming";
                        FilesNotRenamedLabel.Content = Done + " Done out of " + TotalFiles;
                    });
                }
                Dispatcher.Invoke((Action)delegate
                {
                    StatusProgressBar.Value = 100;
                    FilesFoundLabel.Content = "All Done";
                    FilesRenamedSuccessLabel.Content = FilesNamesFoundNEW.Count + " Renamed";
                    FilesNotRenamedLabel.Content = FilesNamesNotFound.Count + " Not Renamed";
                });
                #endregion
                //ALL DONE
            }).Start();
        }

        private string ProcessNumber(string episode)
        {
            int EpiNum;
            if (int.TryParse(episode, out EpiNum))
            {
                if (EpiNum < 10)
                {
                    episode = "0" + episode;
                    return episode;
                }
                else return episode;
            }
            else
                return episode;
        }

        private bool ISEPISODE(string token)
        {
            token = token.Trim().ToLower();
            #region Does not have Num
            if (!ContainsNumber(token))
                return false;
            #endregion
            #region Has Num
            else
            {
                string Temp;
                int SeasonNum;
                int EpisodeNum;
                if (token.StartsWith("s") && token.Length == 3)
                {
                    try
                    {
                        Temp = token.Substring(1, 2);
                        if (int.TryParse(Temp, out SeasonNum))
                        {
                            Season = SeasonNum.ToString();
                            if (Episode == "") return false;
                            else return true;
                        }
                    }
                    catch { }
                }
                try
                {
                    if (token.Contains("{") && token.StartsWith("s"))
                    {
                        Temp = token.Substring(token.IndexOf("{"));
                        Temp = Temp.Remove(0, 1);
                        Temp = Temp.Substring(0, Temp.IndexOf("}"));
                        if (int.TryParse(Temp, out SeasonNum))
                        {
                            Season = SeasonNum.ToString();
                            if (Episode == "") return false;
                            else return true;
                        }
                    }
                }
                catch { }
                if (token.StartsWith("e") && token.Length == 3)
                {
                    try
                    {
                        Temp = token.Substring(1, 2);
                        if (int.TryParse(Temp, out EpisodeNum))
                        {
                            Episode = EpisodeNum.ToString();
                            if (Season == "") return false;
                            else return true;
                        }
                    }
                    catch { }
                }
                try
                {
                    if (token.Contains("{") && token.StartsWith("e"))
                    {
                        Temp = token.Substring(token.IndexOf("{"));
                        Temp = Temp.Remove(0, 1);
                        Temp = Temp.Substring(0, Temp.IndexOf("}"));
                        if (int.TryParse(Temp, out EpisodeNum))
                        {
                            Episode = EpisodeNum.ToString();
                            if (Season == "") return false;
                            else return true;
                        }
                    }
                }
                catch { }
                try
                {
                    if (token.Length == 5)
                    {
                        Temp = token.Substring(0, 2);
                        if (int.TryParse(Temp, out SeasonNum))
                        {
                            Temp = token.Substring(3, 2);
                            if (int.TryParse(Temp, out EpisodeNum))
                            {
                                Season = SeasonNum.ToString();
                                Episode = EpisodeNum.ToString();
                                return true;
                            }
                        }
                    }
                }
                catch { }
                try
                {
                    if (token.Length == 6)
                    {
                        Temp = token.Substring(1, 2);
                        if (int.TryParse(Temp, out SeasonNum))
                        {
                            Temp = token.Substring(4, 2);
                            if (int.TryParse(Temp, out EpisodeNum))
                            {
                                Season = SeasonNum.ToString();
                                Episode = EpisodeNum.ToString();
                                return true;
                            }
                        }
                    }
                }
                catch { }
            }
            #endregion

            return false;
        }

        #region Special Character Checker
        private bool ContainsNumber(string token)
        {
            if (token.Contains("1") || token.Contains("2") ||
                token.Contains("3") || token.Contains("4") ||
                token.Contains("5") || token.Contains("6") ||
                token.Contains("7") || token.Contains("8") ||
                token.Contains("9") || token.Contains("0")
                )
                return true;
            else return false;
        }
        private bool ContainsPunctuation(string token)
        {
            if (token.Contains("<") ||
                token.Contains(",") ||
                token.Contains(">") ||
                token.Contains(".") ||
                token.Contains("?") ||
                token.Contains("/") ||
                token.Contains("!") ||
                token.Contains("@") ||
                token.Contains("#") ||
                token.Contains("$") ||
                token.Contains("%") ||
                token.Contains("^") ||
                token.Contains("&") ||
                token.Contains("*") ||
                token.Contains("(") ||
                token.Contains(")") ||
                token.Contains("-") ||
                token.Contains("_") ||
                token.Contains("+") ||
                token.Contains("=") ||
                token.Contains("`") ||
                token.Contains("~") ||
                token.Contains("{") ||
                token.Contains("}") ||
                token.Contains("[") ||
                token.Contains("]") ||
                token.Contains(":") ||
                token.Contains(";") ||
                token.Contains("\"") ||
                token.Contains("'")
                )
                return true;
            else
                return false;
        }
        #endregion
        private bool isVideoFile(string Path)
        {
            var Extention = Path.Split('.')[Path.Split('.').Length - 1].ToLower();
            switch (Extention)
            {
                case "aaf":
                case "3gp":
                case "asf":
                case "avchd":
                case "avi":
                case "cam":
                case "dat":
                case "dsh":
                case "dvr-ms":
                case "flv":
                case "mpeg-1":
                case "mpeg-2":
                case "mpeg":
                case "m1v":
                case "m2v":
                case "fla":
                case "flr":
                case "sol":
                case "m4v":
                case "mkv":
                case "wrap":
                case "mng":
                case "mov":
                case "mpg":
                case "mpe":
                case "mp4":
                case "mxf":
                case "roq":
                case "nsv":
                case "ogg":
                case "rm":
                case "svi":
                case "smi":
                case "wmv":
                case "wtv":
                case "yut":
                    return true;
                default:
                    return false;
            }
        }
        private T GetJasonData<T>(string url) where T : new()
        {
            using (var w = new WebClient())
            {
                var json_data = string.Empty;
                // attempt to download JSON data as a string
                try
                {
                    json_data = w.DownloadString(url);
                }
                catch (Exception) { }
                // if string with JSON data is not empty, deserialize it to class and return its instance 
                return !string.IsNullOrEmpty(json_data) ? JsonConvert.DeserializeObject<T>(json_data) : new T();
            }
        }
    }

    public class IMDBDETAILS
    {
        public string Title { get; set; }
        public string Year { get; set; }
        public string Rated { get; set; }
        public string Released { get; set; }
        public string Runtime { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public string Writer { get; set; }
        public string Actors { get; set; }
        public string Plot { get; set; }
        public string Language { get; set; }
        public string Country { get; set; }
        public string Awards { get; set; }
        public string Poster { get; set; }
        public string Metascore { get; set; }
        public string imdbRating { get; set; }
        public string imdbVotes { get; set; }
        public string imdbID { get; set; }
        public string Type { get; set; }
        public string Response { get; set; }
    }
    public class FarooRootobject
    {
        public FarooSearchResults[] results { get; set; }
        public string query { get; set; }
        public object[] suggestions { get; set; }
        public int count { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public string time { get; set; }
    }
    public class FarooSearchResults
    {
        public string title { get; set; }
        public string kwic { get; set; }
        public string content { get; set; }
        public string url { get; set; }
        public string iurl { get; set; }
        public string domain { get; set; }
        public string author { get; set; }
        public bool news { get; set; }
        public string votes { get; set; }
        public long date { get; set; }
        public object[] related { get; set; }
    }

}
