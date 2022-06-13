using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace FilmlistEditor
{
    class Program
    {
        readonly static string oneDriveFile = Environment.ExpandEnvironmentVariables(@"%homepath%\OneDrive\Share\Filmlist\filmlist.txt");
        readonly static string archiveDir = @"E:\Archive\";
        readonly static string flag = "_WATCH";

        static void Main(string[] args)
        {
            //Console menu
            while (true)
            {
                Console.Clear();
                DisplayMenu();
                string currentString = Console.ReadLine();

                //Takes the names of all folders with set flag ('_WATCH') in origin directory and writes them to a txt file on OneDrive
                if (currentString.Equals("write"))
                {
                    Console.Clear();
                    CreateListToFile();
                    Console.WriteLine("\nPress any key to continue");
                    Console.ReadKey();
                }
                //Reads all relevant movie names from txt file and creates adequate folders in the origin directory
                else if (currentString.Equals("read"))
                {
                    Console.Clear();
                    CreateListToDir();
                    Console.WriteLine("\nPress any key to continue");
                    Console.ReadKey();
                }
                //Exit the console application
                else if (currentString.Equals("exit"))
                {
                    Environment.Exit(0);
                }
                //Open the desired txt file
                else if (currentString.Equals("txt"))
                {
                    Process.Start(oneDriveFile);
                }
                //Open the selected origin directory in Win-Explorer
                else if (currentString.Equals("dir"))
                {
                    Process.Start("explorer.exe", archiveDir);
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Please use one of the relevant commands");
                    Console.WriteLine("\nPress any key to continue");
                    Console.ReadKey();
                }
            }
        }

        static void DisplayMenu()
        {
            Console.WriteLine("write: Write folder names to txt file");
            Console.WriteLine("read: Create folders based on txt file content");
            Console.WriteLine("txt: Open the relevant txt file");
            Console.WriteLine("dir: Open the relevant destination movie directory");
            Console.WriteLine("exit: Exit the application\n");
        }

        /// <summary>
        /// Scan directory and write collected list data to txt file
        /// </summary>
        /// <remarks>When adding names to the txt file always add in this manner "[orderNumber]. [moviename without special chars] [release year] |"</remarks>
        static void CreateListToFile()
        {
            StreamWriter writer = new StreamWriter(oneDriveFile);

            try
            {
                int itemsInList = GetListAllFromDir(archiveDir).Count;

                foreach (string item in GetListAllFromDir(archiveDir))
                {
                    writer.WriteLine($"{item}");
                }

                writer.WriteLine($"#\n#-----------------------\n#{itemsInList} correctly added items are currently in the list.\n#\n#Insert format: '<order number>. <movie name> <release year> |'");

                Console.WriteLine($"Data has been written to txt file successfully!\n\n{itemsInList} entries have been written to the txt file.");

                System.Diagnostics.Process.Start(oneDriveFile);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Data couldn't be written to txt file\n\nInternal error message: {e}");
            }
            finally
            {
                writer.Close();
            }
        }

        /// <summary>
        /// Collect data (names) from txt file and create folders/subdirectories in original parent directory "archiveDir"
        /// </summary>
        static void CreateListToDir()
        {
            try
            {
                List<string> nameList = GetListAllFromFile(oneDriveFile);
                string fullPath, standardPath;
                int counter = 0;

                for (int i = 0; i < nameList.Count; i++)
                {
                    fullPath = archiveDir + flag + " " + nameList[i] + @"\";
                    standardPath = archiveDir + nameList[i] + @"\";

                    if (!Directory.Exists(fullPath) && !Directory.Exists(standardPath))
                    {
                        Directory.CreateDirectory(fullPath);
                        Console.WriteLine($"Directory '{fullPath}' was successfully created at {Directory.GetCreationTime(fullPath)}.\n");
                        counter++;
                    }
                }

                if(counter > 0)
                {
                    Console.WriteLine($"{counter} items have been added to the origin directory");
                    System.Diagnostics.Process.Start("explorer.exe", archiveDir);
                }
                else if(counter == 0)
                {
                    Console.WriteLine("No changes have been made to the origin directory");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Method to retrieve the relevant subdirectory names
        /// </summary>
        /// <param name="path">Path of the parent directory to retreive its relevant subdirectory names</param>
        /// <returns>List of relevant subdirectory names</returns>
        static List<string> GetListAllFromDir(string path)
        {
            int counter = 0;
            List<string> moviesList = new List<string>(Directory.GetDirectories(path));
            List<string> finalList = new List<string>();
            for (int i = 0; i < moviesList.Count; i++)
            {
                if (moviesList[i].Contains(flag))
                {
                    counter++;
                    finalList.Add($"{(counter < 10 ?  $" {counter.ToString()}." : $"{counter.ToString()}.")} {moviesList[i].Substring(moviesList[i].IndexOf(flag) + 7)}" +
                        $" | (created: {Directory.GetCreationTime(moviesList[i])})"/* + $"---last access: {Directory.GetLastAccessTime(moviesList[i])})"*/);
                }
            }
            return finalList;
        }

        /// <summary>
        /// Method to retrieve the content of the txt file
        /// </summary>
        /// <param name="path">Path of the referenced txt file</param>
        /// <returns>txt file content as a string list</returns>
        static List<string> GetListAllFromFile(string path)
        {
            StreamReader reader = new StreamReader(path);
            List<string> readList = new List<string>();
            string currentLine;
            
            try
            {
                while (!reader.EndOfStream)
                {
                    currentLine = reader.ReadLine();
                    
                    if (!currentLine.Equals(""))
                    {
                        if (currentLine.ElementAt(0).Equals('#'))
                        {
                            continue;
                        }
                        //checks if the current line matches the set rules of a movie entry, also checks if the line is relevant or not, lines with "$" are comments
                        else if (NamingIntegrity(currentLine))
                        {
                            readList.Add(currentLine.Substring(currentLine.IndexOf(". ") + 2, currentLine.IndexOf(" |") - 2 - currentLine.IndexOf(". ")).Trim());
                        }
                        else
                        {
                            Console.WriteLine($"Current line does not match the set entry rules:{currentLine} => please correct the line");
                        }
                    } 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something went wrong while retreiving the contents of the txt file, check sourcecode\n\nInternal error message:\n{e}");
            }
            finally
            {
                reader.Close();
            }

            return readList;
        }

        /// <summary>
        /// Checks if the current input line from the stream reader matches the given integrity rule how a movie entry should look
        /// </summary>
        /// <param name="line">Current line from stream reader</param>
        /// <returns>true or false</returns>
        static bool NamingIntegrity(string line)
        {
            int counterSpace = 0, counterLine = 0, correctString = 0;

            if (!line.ElementAt(0).Equals('#') && (char.IsDigit(line.ElementAt(0)) || line.ElementAt(0).Equals(' ')) &&
                char.IsDigit(line.ElementAt(1)) && line.ElementAt(2).Equals('.') && line.ElementAt(3).Equals(' ') && line.Contains(" |"))
            {
                foreach (char item in line)
                {
                    if (item == ' ' && counterSpace == 0)
                    {
                        counterSpace++;
                    }
                    else if (item == ' ' && counterSpace >= 0)
                    {
                        counterSpace = 0;
                    }
                    else if (item == '|' && counterSpace == 1)
                    {
                        correctString++;
                        counterLine++;
                    }
                    else if (item == '|' && counterSpace == 0)
                    {
                        counterLine++;
                        counterSpace = 0;
                    }
                    else
                    {
                        counterSpace = 0;
                    }
                }

                if (correctString == 1 && counterLine == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
