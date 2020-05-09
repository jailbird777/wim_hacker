using Microsoft.Win32;
using Microsoft.Win32.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace install_wim_tweak
{
    internal class Program
    {
        private const string HIVE_MOUNT_DIR = "wim_hacker";
        private static readonly string _pkgDirectory = HIVE_MOUNT_DIR + "\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\";
        private const string HIVE_MOUNT_POINT = "HKLM\\" + HIVE_MOUNT_DIR;
        private const string REGISTRY_PATH = "Windows\\system32\\config\\SOFTWARE";
        private static readonly string ProgramHeader =
        "-------------------------------------------\n" +
        "------------WIM Hacker v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + "------------\n" +
        "-----------for Windows 6.x/10.x------------\n" +
        "----------------By JailBird----------------\n" +
        "-------------------------------------------\n" +
        "-------Based on win6x_registry_tweak-------\n" +
        "---------Created by Michał Wnuowski--------\n" +
        "-----Concept by Aviv00@msfn / lite8@MDL----\n" +
        "-----------Modified by Legolash2o----------\n" +
        "-------------------------------------------\n\n";
        private const string PROGRAM_HELP_INFO =
        "USAGE : \n" +
        "   wim_hacker /p <Path> [/f <FileName> || /l] [/?]\n\n" +
        "REMARKS : \n" +
        "   /p <Path>     Use '/p' switch to provide path to mounted install.wim\n" +
        "   /f <FileName> Use '/f' switch to provide path to file containing a\n" +
        "                 list of packages to remove, one per line\n" +
        "   /?            Use '/?' switch to display this info\n" +
        "   /l            Outputs all packages to \"Packages.txt\"\n" +
        "EXAMPLE : \n" +
        "    wim_hacker /p C:\\temp files\\mount\n /l" +
        "    wim_hacker /p C:\\temp files\\mount\n /f c:\\packges.txt";
        private static string _hiveFileInfo;
        private static string _packageFile = "";
        private static Dictionary<char, string> _cmdLineArgs;
        private static readonly string PackLog = Environment.CurrentDirectory + "\\Packages.txt";
        private static string _cr = "";

        //static Sid SysUser = new Sid();

        private static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ProgramHeader);
            Console.ResetColor();

            try
            {
                _cmdLineArgs = ProcessCmdArgs(args, new char[] { '?', 'f', 'l', 'p' });

                if (_cmdLineArgs.ContainsKey('?'))
                {
                    Console.Write(PROGRAM_HELP_INFO);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("\nPlease make sure you use lowercase for the /f, /l, and /p");
                    Console.ResetColor();
                    Environment.Exit(1);
                }

                if (!_cmdLineArgs.ContainsKey('f') || string.IsNullOrEmpty(_cmdLineArgs['f']))
                {
                    if (!_cmdLineArgs.ContainsKey('l'))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("\n/f option not specified.  Please use /? for help");
                        Console.ResetColor();
                        Environment.Exit(1);
                    }
                }
                else
                {
                    _packageFile = Path.Combine(_cmdLineArgs['f'], "");
                }

                if (!_cmdLineArgs.ContainsKey('p') || string.IsNullOrEmpty(_cmdLineArgs['p']))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("\n/p option not specified.  Please use /? for help");
                    Console.ResetColor();
                    Environment.Exit(1);
                }

                _hiveFileInfo = Path.Combine(_cmdLineArgs['p'], REGISTRY_PATH);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("MountPath : {0}", "\"" + _cmdLineArgs['p'] + "\"");
                Console.ResetColor();

                if (string.IsNullOrEmpty(_hiveFileInfo))
                {
                    Environment.Exit(-2);
                }

                if (!File.Exists(_hiveFileInfo))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Registry file not found, please make sure your mount path is correct!");
                    Console.ResetColor();
                    Environment.Exit(-532459699);
                }

                if (_cmdLineArgs.ContainsKey('l'))
                {
                    Console.Write("Writing to Log (Packages.txt)         ");
                    if (File.Exists(PackLog)) { File.Delete(PackLog); }
                    ListComponentSubkeys(_pkgDirectory + "Packages\\");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("OK");
                    Console.ResetColor();
                    Ending();
                }

                if (!File.Exists(_packageFile))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Package file not found, please make sure your package file path is correct!");
                    Console.ResetColor();
                    Environment.Exit(-532459698);
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\n------------------Starting-----------------");
                Console.ResetColor();

                Console.Write("Reading package list...                  ");
                string[] _comp = File.ReadAllLines(_packageFile);
                for (int i = 0; i < _comp.Length; i++)
                {
                    while (_comp[i].Contains("~"))
                    {
                        _comp[i] = _comp[i].Substring(0, _comp[i].Length - 1);
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();

                Console.Write("Mounting registry file...                ");
                if (!Contains<string[], string>(Registry.LocalMachine.GetSubKeyNames(), HIVE_MOUNT_DIR))
                {
                    if (!LoadHive(_hiveFileInfo, HIVE_MOUNT_POINT))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("FAIL");
                        Console.ResetColor();
                        Ending();
                    }
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();

                Console.Write("Taking Ownership...                      ");

                AccessTokenProcess myProcToken = new AccessTokenProcess(Process.GetCurrentProcess().Id, TokenAccessType.TOKEN_ALL_ACCESS | TokenAccessType.TOKEN_ADJUST_PRIVILEGES);
                myProcToken.EnablePrivilege(new TokenPrivilege(TokenPrivilege.SE_TAKE_OWNERSHIP_NAME, true));

                if (Win32.GetLastError() != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FAIL");
                    Console.WriteLine("You must be logged as Administrator.");
                    Console.ResetColor();
                    Ending();
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();

                Console.Write("Editing \'Packages\' subkeys            ");
                try
                {
                    if (CleanComponentSubkeys(_pkgDirectory + "Packages\\", _comp, false))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("OK");
                        Console.ResetColor();
                    }
                }
                catch { }
                Console.Write("Editing \'PackagesPending\' subkeys     ");
                try
                {
                    if (CleanComponentSubkeys(_pkgDirectory + "PackagesPending\\", _comp, true))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("OK");
                        Console.ResetColor();
                    }
                }
                catch { }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Modifying registry completed sucessfully.");
                Console.ResetColor();

                if (Contains<string[], string>(Microsoft.Win32.Registry.LocalMachine.GetSubKeyNames(), HIVE_MOUNT_DIR))
                {
                    Console.Write("Unmounting key...                        ");
                    if (!UnloadHive(HIVE_MOUNT_POINT))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("FAIL");
                        Console.WriteLine("You must unmount registry hive manually.");
                        Console.WriteLine("Hit any key to close.");
                        Console.ResetColor();
                        Console.ReadKey();
                        Environment.Exit(-3);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("OK");
                    Console.ResetColor();
                }

                Console.Write("Removing \'Packages\'...                ");
                if (RemoveComponentSubkeys(_pkgDirectory + "Packages\\", _comp))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("OK");
                    Console.WriteLine("Removed packages successfully.");
                    Console.ResetColor();
                }

                Console.Write("Removing \'PackagesPending\'...         ");
                if (RemoveComponentSubkeys(_pkgDirectory + "Packages\\", _comp))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("OK");
                    Console.WriteLine("Removed packages successfully.");
                    Console.ResetColor();
                }
                Ending();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL");
                Console.WriteLine("Unhandled error occured.");
                Console.ResetColor();
                Console.WriteLine(ex.Message);
                Ending();
            }
        }

        private static bool RemoveComponentSubkeys(string registryPath, string[] CR)
        {
            int consoleX = 0; int consoleY = 0;
            try
            {
                consoleX = Console.CursorLeft; consoleY = Console.CursorTop;
            }
            catch { }

            int count = 1;
            int tot = 0;
            string PackL = _hiveFileInfo;
            while (!PackL.EndsWith("\\Windows\\"))
            {
                PackL = PackL.Substring(0, PackL.Length - 1);
            }

            PackL = PackL.Replace("Windows\\", "");

            foreach (string subkeyname in _cr.Split(Environment.NewLine.ToCharArray()))
            {
                if (CR != null)
                {
                    foreach (string Comp in CR)
                    {
                        if (subkeyname.StartsWith(Comp) && !string.IsNullOrEmpty(Comp))
                        {
                            tot += 1;
                        }
                    }
                }
            }

            foreach (string subkeyname in _cr.Split(Environment.NewLine.ToCharArray()))
            {
                foreach (string Comp in CR)
                {
                    if (subkeyname.StartsWith(Comp) && !string.IsNullOrEmpty(Comp))
                    {
                        CorrectConsolePostion(tot, consoleY, consoleX);


                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("{0}/{1}", count, tot);
                        Console.ResetColor();
                        try
                        {
                            Process RC = new Process();
                            RC.StartInfo.FileName = "pkgmgr.exe";
                            RC.StartInfo.Arguments = "/o:" + "\"" + PackL + ";" + PackL + "Windows" + "\"" + " /up:" + subkeyname + " /norestart /quiet";
                            RC.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                            RC.Start();
                            RC.WaitForExit();
                        }
                        catch (Exception Ex) { Console.WriteLine(Ex.Message); }
                        count++;
                    }
                }
            }

            return true;
        }

        private static bool ListComponentSubkeys(string registryPath)
        {
            int consoleX = 0; int consoleY = 0;
            try
            {
                consoleX = Console.CursorLeft; consoleY = Console.CursorTop;
            }
            catch { }

            RegistryKey MyKey = Registry.LocalMachine.OpenSubKey(registryPath);
            int count = 1;
            int tot = 0;
            string PackL = "";

            foreach (string subkeyname in MyKey.GetSubKeyNames())
            {
                if (!subkeyname.StartsWith("Package"))
                {
                    tot += 1;
                }
            }

            foreach (string subkeyname in MyKey.GetSubKeyNames())
            {
                if (!subkeyname.StartsWith("Package"))
                {
                    CorrectConsolePostion(tot, consoleY, consoleX);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("{0}/{1}", count, tot);
                    Console.ResetColor();
                    PackL += subkeyname + Environment.NewLine;
                    count++;
                }
            }

            MyKey.Close();
            try
            {
                StreamWriter SW = new StreamWriter(PackLog, true);
                SW.WriteLine(PackL);
                SW.Close();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("FAIL");
                Console.ResetColor();
            }
            return true;
        }

        private static bool CleanComponentSubkeys(string registryPath, string[] CN, bool skipFail)
        {
            int consoleX = 0; int consoleY = 0;
            try
            {
                consoleX = Console.CursorLeft; consoleY = Console.CursorTop;
            }
            catch { }
            try
            {
                RegistryKey MyKey = Registry.LocalMachine.OpenSubKey(registryPath);
                IdentityReference CurUser = System.Security.Principal.WindowsIdentity.GetCurrent().User;
                int count = 1;
                int tot = 0;

                foreach (string subkeyname in MyKey.GetSubKeyNames())
                {
                    foreach (string comp in CN)
                    {
                        if (subkeyname.Contains(comp))
                        {
                            tot += 1;
                        }
                    }
                }

                Debug.Assert(MyKey != null, "myKey != null");

                foreach (string subkeyname in MyKey.GetSubKeyNames())
                {
                    foreach (string comp in CN)
                    {
                        if (subkeyname.Contains(comp))
                        {
                            try
                            {
                                if (!_cr.Contains(subkeyname))
                                {
                                    _cr += subkeyname + Environment.NewLine;
                                }

                                CorrectConsolePostion(tot, consoleY, consoleX);
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write("{0}/{1}", count, tot);
                                Console.ResetColor();
                                if (!RegSetOwneship(MyKey, subkeyname, CurUser))
                                {
                                    MyKey.Close();
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("FAIL");
                                    Console.WriteLine("Error at setting key privileges.");
                                    Console.ResetColor();
                                    Ending();
                                }
                                RegistryAccessRule ntmp = RegSetFullAccess(MyKey, subkeyname, CurUser);
                                RegistryKey nSubKey = MyKey.OpenSubKey(subkeyname, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
                                try
                                {
                                    if (Contains<string[], string>(nSubKey.GetValueNames(), "Visibility"))
                                    {
                                        if (!Contains<string[], string>(nSubKey.GetValueNames(), "DefVis"))
                                        {
                                            nSubKey.SetValue("DefVis", nSubKey.GetValue("Visibility"), RegistryValueKind.DWord);
                                        }

                                        nSubKey.SetValue("Visibility", 0x00000001, RegistryValueKind.DWord);
                                    }

                                    if (Contains<string[], string>(nSubKey.GetSubKeyNames(), "Owners"))
                                    {
                                        RegSetOwneship(MyKey, subkeyname + "\\Owners", CurUser);
                                        RegSetFullAccess(MyKey, subkeyname + "\\Owners", CurUser);
                                        nSubKey.DeleteSubKey("Owners");
                                    }
                                }
                                catch (Exception Ex) { Console.WriteLine(Ex.Message); }
                                nSubKey.Close();
                                RegRemoveAccess(MyKey, subkeyname, CurUser, ntmp);
                            }
                            catch { }
                            count++;
                        }
                    }
                }

                MyKey.Close();
            }
            catch
            {
                if (skipFail)
                {
                    return true;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   FAIL - Key doesn't exist");
                Console.ResetColor();
                return false;
            }
            return true;
        }

        private static void Ending()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n-------------------Ending------------------");
            Console.ResetColor();
            if (Contains<string[], string>(Microsoft.Win32.Registry.LocalMachine.GetSubKeyNames(), HIVE_MOUNT_DIR))
            {
                Console.Write("Unmounting key...                        ");
                if (!UnloadHive(HIVE_MOUNT_POINT))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FAIL");
                    Console.WriteLine("You must unmount registry hive manually.");
                    Console.WriteLine("Hit any key to close.");
                    Console.ResetColor();
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();
            }
            Environment.Exit(0);
        }

        private static void RegRemoveAccess(RegistryKey nParentKey, string nkey, IdentityReference nuser, RegistryAccessRule nacc)
        {
            RegistryKey nSubKey = nParentKey.OpenSubKey(nkey, RegistryKeyPermissionCheck.ReadWriteSubTree,
                RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            RegistrySecurity nSubKeySec = nSubKey.GetAccessControl(AccessControlSections.Access);
            nSubKeySec.RemoveAccessRule(nacc);
            nSubKey.SetAccessControl(nSubKeySec);
            nSubKey.Close();
        }

        private static RegistryAccessRule RegSetFullAccess(RegistryKey nParentKey, string nkey, IdentityReference nuser)
        {
            RegistryKey nSubKey = null;
            try
            {
                nSubKey = nParentKey.OpenSubKey(nkey, RegistryKeyPermissionCheck.ReadWriteSubTree,
                RegistryRights.ReadKey | RegistryRights.ChangePermissions | RegistryRights.ReadPermissions);
                RegistrySecurity nSubKeySec = nSubKey.GetAccessControl(AccessControlSections.Access);
                RegistryAccessRule nAccRule = new RegistryAccessRule(nuser, RegistryRights.FullControl, AccessControlType.Allow);
                nSubKeySec.AddAccessRule(nAccRule);
                //nSubKeySec.RemoveAccessRul
                nSubKey.SetAccessControl(nSubKeySec);
                nSubKey.Close();
                return nAccRule;
            }
            catch
            {
                nSubKey?.Close();
                return null;
            }
        }

        private static bool RegSetOwneship(RegistryKey nParentKey, string nkey, IdentityReference nuser)
        {
            RegistryKey nSubKey = null;
            try
            {
                nSubKey = nParentKey.OpenSubKey(nkey, RegistryKeyPermissionCheck.ReadWriteSubTree,
                RegistryRights.TakeOwnership | RegistryRights.ReadKey | RegistryRights.ReadPermissions);
                RegistrySecurity nSubKeySec = nSubKey.GetAccessControl(AccessControlSections.Owner);
                nSubKeySec.SetOwner(nuser);
                nSubKey.SetAccessControl(nSubKeySec);
                nSubKey.Close();
                return true;
            }
            catch
            {
                if (nSubKey != null)
                {
                    nSubKey.Close();
                }

                return false;
            }
        }

        private static bool Contains<typeColl, typeKey>(typeColl collection, typeKey val)
            where typeColl : IEnumerable<typeKey>
            where typeKey : IComparable
        {
            foreach (typeKey subelement in collection)
            {
                if (subelement.CompareTo(val) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InitProcess(Process nproc)
        {
            nproc.StartInfo.UseShellExecute = false;
            nproc.StartInfo.RedirectStandardError = true;
            nproc.StartInfo.RedirectStandardOutput = true;
            nproc.StartInfo.RedirectStandardInput = true;
            nproc.StartInfo.CreateNoWindow = true;
        }

        private static bool LoadHive(string nfile, string nkeyname)
        {
            return RunReg(string.Format("LOAD {0} {1}", nkeyname, "\"" + nfile + "\""));
        }

        private static bool UnloadHive(string nkeyname)
        {
            return RunReg(string.Format("UNLOAD {0}", nkeyname));
        }

        private static bool RunReg(string nArguments)
        {
            Process reg = new Process();
            InitProcess(reg);
            reg.StartInfo.FileName = "reg.exe";
            reg.StartInfo.Arguments = nArguments;
            reg.Start();
            reg.WaitForExit();
            string RegOutput = reg.StandardOutput.ReadToEnd();
            string RegError = reg.StandardError.ReadToEnd();
            if ((RegOutput.Length < 1) || (RegError.Length > 1))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static void CorrectConsolePostion(int tot, int consoleY, int consoleX)
        {
            try
            {
                Console.CursorLeft = consoleX;
                Console.CursorTop = consoleY;

                if (tot < 10)
                {
                    Console.CursorLeft = consoleX;
                    Console.CursorTop = consoleY;
                }
                if (tot > 9 && tot < 100)
                {
                    Console.CursorLeft = consoleX - 2;
                    Console.CursorTop = consoleY;
                }
                if (tot > 99 && tot < 1000)
                {
                    Console.CursorLeft = consoleX - 4;
                    Console.CursorTop = consoleY;
                }
                if (tot > 999 && tot < 10000)
                {
                    Console.CursorLeft = consoleX - 6;
                    Console.CursorTop = consoleY;
                }
            }
            catch
            {
            }
        }

        private static Dictionary<char, string> ProcessCmdArgs(string[] args, char[] allowedArgs)
        {
            Dictionary<char, string> tmp = new Dictionary<char, string>();
            string curV = "";
            string argtmp;
            char curK = ' ';
            foreach (string arg in args)
            {
                argtmp = arg.Trim();
                if (argtmp[0] == '/')
                {
                    if (Contains<char[], char>(allowedArgs, argtmp[1]))
                    {
                        if (curK != ' ')
                        {
                            tmp.Add(curK, curV.Trim());
                        }

                        curK = arg[1];
                        curV = "";
                    }
                    else
                    {
                        tmp.Clear();
                        tmp.Add('?', "");
                        return tmp;
                    }
                }
                else
                {
                    if (curK == ' ')
                    {
                        tmp.Clear();
                        tmp.Add('?', "");
                        return tmp;
                    }
                    curV += " " + argtmp;
                }
            }
            tmp.Add(curK, curV.Trim());
            return tmp;
        }
    }
}
#region License
/* Copyright (c) 2008 Michał Wnuowski
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */
#endregion

#region Contact
/*
 * Michał Wnuowski
 * Email: wnuku1@hotmail.com
 */
#endregion
