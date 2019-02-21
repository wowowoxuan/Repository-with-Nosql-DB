///////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - GUI for Project3HelpWPF                      //
// ver 1.0                                                           //
// author Weiheng Chai                                               //
// source Jim Fawcett, Ammar                                         //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provides a WPF-based GUI for Project3HelpWPF demo.  It's 
 * responsibilities are to:
 * - Provide a display of directory contents of a remote ServerPrototype.
 * - It provides a subdirectory list and a filelist for the selected directory.
 * - You can navigate into subdirectories by double-clicking on subdirectory
 *   or the parent directory, indicated by the name "..".
 *   
 * Required Files:
 * ---------------
 * Mainwindow.xaml, MainWindow.xaml.cs
 * Translater.dll
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 30 Mar 2018
 * - first release
 * - Several early prototypes were discussed in class. Those are all superceded
 *   by this package.
 */

// Translater has to be statically linked with CommLibWrapper
// - loader can't find Translater.dll dependent CommLibWrapper.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using MsgPassingCommunication;
using System.IO;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Stack<string> pathStack_ = new Stack<string>();
        private Translater translater;
        private CsEndPoint endPoint_;
        private Thread rcvThrd = null;
        private Dictionary<string, Action<CsMessage>> dispatcher_
          = new Dictionary<string, Action<CsMessage>>();

        //----< process incoming messages on child thread >----------------

        private void processMessages()
        {
            ThreadStart thrdProc = () => {
                while (true)
                {
                    CsMessage msg = translater.getMessage();
                    string msgId = msg.value("command");
                    if (dispatcher_.ContainsKey(msgId))
                        dispatcher_[msgId].Invoke(msg);
                }
            };
            rcvThrd = new Thread(thrdProc);
            rcvThrd.IsBackground = true;
            rcvThrd.Start();
        }

        private void clearQuery()
        {
            browsequery.Items.Clear();
        }

        private void clearFiles()
        {

            checkoutfiles.Items.Clear();
            adddependencies.Items.Clear();
            checkindependency.Items.Clear();
        }
        //----< function dispatched by child thread to main thread >-------

        private void addFile(string file)
        {

            checkoutfiles.Items.Add(file);
            checkindependency.Items.Add(file);
            adddependencies.Items.Add(file);

        }
        private void addquery(string file)
        {
            browsequery.Items.Add(file);
        }
        //----< add client processing for message with key >---------------

        private void addClientProc(string key, Action<CsMessage> clientProc)
        {
            dispatcher_[key] = clientProc;
        }
        private void clearshowopenfile()
        {
            notclosedfiles.Items.Clear();
        }
        private void showopenfile(string file)
        {
            notclosedfiles.Items.Add(file);
        }

        private void Dispatchershowopen()
        {
            Action<CsMessage> showopen = (CsMessage rcvMsg) =>
            {
                Action clrFiles = () =>
                {
                    Console.Write("\n receive server's reply to the get list of check in open files from" + rcvMsg.value("from") + " to " + rcvMsg.value("to"));
                    clearshowopenfile();
                    statusBarText.Text = "receive a message, message command is show file that check in open";
                };
                Dispatcher.Invoke(clrFiles, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("statusopen"))
                    {
                        Action<string> doFile = (string file) =>
                        {
                            showopenfile(file);
                        };
                        Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("showopen", showopen);
        }
        private void clearshowclosefile()
        {
            adddependencies.Items.Clear();
        }
        private void adddependent(string file)
        {
            adddependencies.Items.Add(file);
        }
        /*deal with the situation that I want to show the files which status is closed, this dispatcher did not used in my design, but I think it will be useful*/
        private void Dispatchershowclose()
        {
            Action<CsMessage> showclose = (CsMessage rcvMsg) =>
            {
                Action clrFiles = () =>
                {
                    clearshowclosefile();
                    statusBarText.Text = "receive a messgae, the command is show the closed files in the dbcore";
                };
                Dispatcher.Invoke(clrFiles, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("statusclose"))
                    {
                        Action<string> doFile = (string file) =>
                        {
                            adddependent(file);
                        };
                        Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("showclose", showclose);
        }
        /*dispatcher the message reply modify*/
        private void Dispatchermodify()
        {
            Action<CsMessage> modify = (CsMessage rcvMsg) =>
            {
                Action showmessage = () =>
                {
                    statusBarText.Text = "receive a message, this message replys modify, if there is no message box,  modify success";
                    Console.Write("\n receive server's reply to the modify from" + rcvMsg.value("from") + " to " + rcvMsg.value("to"));
                };
                Dispatcher.Invoke(showmessage, new Object[] { });

                Console.WriteLine("test modify and check in success");
                if (rcvMsg.value("result") == "false")
                {
                    MessageBox.Show("only the author can modify the file!");
                }


            };
            addClientProc("modify", modify);
        }
        /*dispatcher the message that reply getfiles*/
        private void DispatcherLoadGetFiles()
        {
            Action<CsMessage> getFiles = (CsMessage rcvMsg) =>
            {
                Action clrFiles = () =>
                {
                    clearFiles();
                    statusBarText.Text = "get repy of message with command getfile, get a list of files in the repository";
                    Console.Write("\n receive server's reply to the getFiles from" + rcvMsg.value("from") + " to " + rcvMsg.value("to"));
                };
                Dispatcher.Invoke(clrFiles, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("file"))
                    {
                        Action<string> doFile = (string file) =>
                        {
                            addFile(file);
                        };
                        Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("getFiles", getFiles);
        }
        /*dispatcher the message close a check in open */
        private void Dispatcherclose()
        {
            Action<CsMessage> close = (CsMessage rcvMsg) =>
            {
                Console.WriteLine("test close checkin success");
                Action clrQuery = () =>
                {
                    showfilesnotclosed_Click(null, null);
                    statusBarText.Text = "receive reply for a message to close a file's check in status";
                    Console.Write("\n receive server's reply to close a file's check in status from" + rcvMsg.value("from") + " to " + rcvMsg.value("to"));
                };
                Dispatcher.Invoke(clrQuery, new Object[] { });
                if (rcvMsg.value("result") == "false")
                {
                    MessageBox.Show("only the author can close the file!");
                }

            };
            addClientProc("close", close);
        }
        /*dispatcher the message that reply query*/
        private void Dispatcherquery()
        {
            Action<CsMessage> getQLists = (CsMessage rcvMsg) =>
            {
                Action clrQuery = () =>
                {
                    clearQuery();
                    statusBarText.Text = "receive a message reply query, the list of files will show in the browse tab";
                };
                Console.WriteLine("\n receive server's reply to the search message from" + rcvMsg.value("from") + " to " + rcvMsg.value("to"));
                Console.WriteLine("\n the files are:");
                Dispatcher.Invoke(clrQuery, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("file"))
                    {
                        Action<string> doFile = (string file) =>
                        {
                            addquery(file);
                            Console.WriteLine("\n" + file);
                        };
                        Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("getQLists", getQLists);
        }
        /*dispatcher the message that reply checkout*/
        private void Dispatchercheckout()
        {
            Action<CsMessage> checkout = (CsMessage rcvMsg) =>
            {
                Action showmessage = () =>
                {

                    statusBarText.Text = "receive a message reply checkout, check out success";
                };
                Console.WriteLine("\n receive server's reply to the check out message from" + rcvMsg.value("from") + " to " + rcvMsg.value("to"));
                Console.WriteLine("\n the receive path is " + System.IO.Path.GetFullPath("../../../../clientstore"));
                Console.WriteLine("\nfile name is " + rcvMsg.value("file"));
                Dispatcher.Invoke(showmessage, new Object[] { });
                Console.WriteLine("\n checkout success!");

            };
            addClientProc("checkout", checkout);
        }
        /*dispatcher the message that reply checkin*/
        private void Dispatchercheckin()
        {
            Action<CsMessage> checkin = (CsMessage rcvMsg) =>
            {
                Action showmessage = () =>
                {
                    statusBarText.Text = "receive a message reply check in, if there is no messagebox, check in success";
                };
                Dispatcher.Invoke(showmessage, new Object[] { });
                Console.WriteLine("\n receive server's reply to the check in message from" + rcvMsg.value("from") + " to " + rcvMsg.value("to"));
                if (rcvMsg.value("replycin") == "authorfalse")
                {
                    MessageBox.Show("you are not the author！");
                }
                else if (rcvMsg.value("replycin") == "open")
                {
                    MessageBox.Show("the file is not close, you cannot checkin new version, go to modify to change it!");
                }
                else
                {
                    Console.WriteLine("\n check in success!");
                }
            };
            addClientProc("checkin", checkin);
        }
        /*dispatcher the message that send the children files*/
        private void Dispatcherdepencies()
        {
            Action<CsMessage> depencyfile = (CsMessage rcvMsg) =>
            {
                Action showmessage = () =>
                {

                    statusBarText.Text = "receive check in children files, the file name is " + rcvMsg.value("file");
                };
                Dispatcher.Invoke(showmessage, new Object[] { });
                Console.WriteLine("\n receive children files, the file name is " + rcvMsg.value("file"));
            };
            addClientProc("depencyfile", depencyfile);
        }


        private void popupcode(string file)
        {
            string path = System.IO.Path.Combine("../../../../popupcode/", file);
            Console.WriteLine(System.IO.Path.GetFullPath(path));
            string contents = File.ReadAllText(path);
            CodePopUp popup = new CodePopUp();
            popup.codeView.Text = contents;
            popup.Show();
        }

        private void Dispatcherviewmetadata()
        {
            Action<CsMessage> viewmetadata = (CsMessage rcvMsg) =>
            {
                Action doit = () =>
                {
                    cleardepencies();
                    statusBarText.Text = "receive message reply view metadata";
                    datatime.Text = rcvMsg.value("datatime");
                    broauthor.Text = rcvMsg.value("bauthor");
                    name.Text = rcvMsg.value("bfilename");
                    descript.Text = rcvMsg.value("descript");
                    popupcode(rcvMsg.value("file"));
                };
                Console.Write("\n receive server's reply to the viewmetadata message from" + rcvMsg.value("from") + " to " + rcvMsg.value("to"));
                Dispatcher.Invoke(doit, new Object[] { });

                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    Action<string> doFile = (string keys) =>
                    {
                        if (keys.Contains("depencies"))
                        {
                            depencies.Items.Add(rcvMsg.value(keys));
                        }
                        if (keys.Contains("category"))
                        {
                            categoriesshow.Items.Add(rcvMsg.value(keys));
                        }
                    };
                    Dispatcher.Invoke(doFile, new Object[] { key });
                }
            };
            addClientProc("viewmetadata", viewmetadata);
        }
        //----< load all dispatcher processing >---------------------------

        private void loadDispatcher()
        {

            DispatcherLoadGetFiles();
            Dispatchercheckin();
            Dispatchercheckout();
            Dispatcherviewmetadata();
            Dispatcherquery();
            Dispatcherdepencies();
            Dispatchershowopen();
            Dispatcherclose();
            Dispatchershowclose();
            Dispatchermodify();
        }
        //----< start Comm, fill window display with dirs and files >------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // start Comm
            Console.WriteLine("--------------GUI2----------------------");
            endPoint_ = new CsEndPoint();
            endPoint_.machineAddress = "localhost";
            endPoint_.port = 8083;
            translater = new Translater();
            translater.listen(endPoint_);

            // start processing messages
            processMessages();

            // load dispatcher
            loadDispatcher();

            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;

            pathStack_.Push("../Storage");
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("path", pathStack_.Peek());
            translater.postMessage(msg);
            msg.add("command", "getFiles");
            translater.postMessage(msg);
            initshowfilelist();


        }
        //----< strip off name of first part of path >---------------------

        private string removeFirstDir(string path)
        {
            string modifiedPath = path;
            int pos = path.IndexOf("/");
            modifiedPath = path.Substring(pos + 1, path.Length - pos - 1);
            return modifiedPath;
        }

        //----< run test on another thread >---------------------------------

        private void test()
        {
            ThreadStart thrdProc = () => {
                Console.WriteLine("\n========================this is test======================================");
                Console.WriteLine("\n-------------------------- using c# and c++, test requirement #1 passed----------------------------");
                testcheckin();
                testcheckout();
                testbrowse();
                testviewmetadata();
                Console.WriteLine("\n-------------------test requirement 2 passed---------------------------");
                Console.WriteLine("\n-------------------test requirement 3 passed---------------------------");
                Console.WriteLine("\n-------------------test requirement 4,5---------------------");
                Console.WriteLine("\n using a message communication channel based on sockets to send and receive message between server and client, when test the requirement 2 and 3 you can see the communication,when communication the server of client can receive and send message at the same time,meet requirement 5");
                Console.WriteLine("\n provide a message-passing communication system, based on Sockets, used to access the Repository's functionality from another process or machine.");
                Console.WriteLine("\n ---------------test requirement 4,5 passed--------------------");
                Console.WriteLine("\n-------------using professor's code to send and receive file, requirement 6 passed,when send and receive file, you can see the path in the console------------------");
                Console.WriteLine("\n========================test end==========================================");
            };
            Thread testThrd = new Thread(thrdProc);
            testThrd.IsBackground = true;
            testThrd.Start();



        }

        /*def tests*/
        private void testbrowse()
        {
            Thread.Sleep(200);
            Action act = () => {
                Console.WriteLine("\n ------------------test browse and view metadata, test requirement 2(c) and 3(c)------------------------ ");
                Console.WriteLine("\n 2(c) when I click the button search, the client will send a request to the server, the server will query the condition in the dbcore, in my design, I only query by filename");
                Console.WriteLine("\n 3(c) when I double click the item in the browse's listbox, the metadata will be showed on the GUI, and the content of the file will be showed in the popup window");
                query.Text = "App";
                RoutedEventArgs m1 = new RoutedEventArgs(null, 0);
                search_Click(this, m1);
            };
            Dispatcher.Invoke(act);
        }
        private void testcheckout()
        {
            Thread.Sleep(200);
            Action act = () => {
                Console.WriteLine("\n ----------------test check out files, test requirement 2(b) and 3(b)-------------------------");
                Console.WriteLine("\n 2(b): when I check out the server will send the file with the reply message to the client");
                Console.WriteLine("\n 3(b): the client receive the file that was sended by server is download");
                checkoutfiles.SelectedIndex = 1;
                Console.WriteLine(checkoutfiles.SelectedItem.ToString());
                RoutedEventArgs m1 = new RoutedEventArgs(null, 0);
                checkout_Click(this, m1);
                Console.WriteLine("-------------------test requirement 2(b) and 3(b) passed-----------------------");
            };
            Dispatcher.Invoke(act);
        }

        private void testcheckin()
        {
            Action act = () => {
                Console.WriteLine("\n --------------------test check in files,test requirement 2(a) and 3(a)----------------------- ");
                Console.WriteLine("\n 2(a): when I check in, the console for the server will show the dbcore which some new element are added to it,you can also see the new file in the listbox for the user to select children");
                Console.WriteLine("\n 3(a): when check in the file, send the file to the server, this do the work upload, you can see the newfile in the listbox for user to select children");
                Console.WriteLine("\n the dbcore was also showed in the server's client");
                checkinfiles.SelectedIndex = 3;
                RoutedEventArgs m1 = new RoutedEventArgs(null, 0);
                checkin_click(this, m1);
                checkinfiles.SelectedIndex = 2;
                checkin_click(this, m1);
                checkinfiles.SelectedIndex = 1;
                checkin_click(this, m1);
                Console.WriteLine("--------------------test requirement 2(a) and 3(a) passed------------------------------");
            };
            Dispatcher.Invoke(act);
        }

        private void cleardepencies()
        {
            depencies.Items.Clear();
            categoriesshow.Items.Clear();
        }



        private void testviewmetadata()
        {
            Thread.Sleep(200);
            Action act = () => {
                browsequery.SelectedIndex = 1;
                MouseButtonEventArgs m2 = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);
                browse_MouseDoubleClick(this, m2);
                Console.WriteLine("--------------------test requirement 2(c) and 3(c) passed------------------------------");
            };
            Dispatcher.Invoke(act);
        }



        private void checkin_click(object sender, RoutedEventArgs e)
        {
            if (checkinfiles.SelectedItem != null)
            {
                CsEndPoint serverEndPoint = new CsEndPoint();
                serverEndPoint.machineAddress = "localhost";
                serverEndPoint.port = 8080;
                CsMessage msg = new CsMessage();
                msg.add("to", CsEndPoint.toString(serverEndPoint));
                msg.add("from", CsEndPoint.toString(endPoint_));
                msg.add("command", "checkin");
                msg.add("parent", "true");
                msg.add("author", author.Text);
                msg.add("description", descrip.Text);
                msg.add("category", cate.Text);
                msg.add("namespace", ns.Text);
                msg.add("file", checkinfiles.SelectedItem.ToString());
                msg.add("sendpath", "../../../../clientstore");
                msg.add("receivepath", "../Storage");
                string abspath = System.IO.Path.GetFullPath("../../../../clientstore");
                Console.WriteLine("\n ----------you click checkin button----------");
                Console.WriteLine("\n send check in message from " + msg.value("from") + " to " + msg.value("to"));
                Console.WriteLine("\n send files from " + msg.value("from") + " to " + msg.value("to"));
                Console.WriteLine("\n send path is " + abspath);
                Console.WriteLine("\n file name is:");
                Console.WriteLine(checkinfiles.SelectedItem.ToString());
                int tempc = 0;
                foreach (var item in checkindependency.SelectedItems)
                {

                    msg.add("children" + tempc.ToString(), item.ToString());
                    tempc++;
                }
                translater.postMessage(msg);
            }

            showfilesnotclosed_Click(null, null);

        }
        /*test end*/
        private void showfile_Click(object sender, RoutedEventArgs e)
        {
            checkinfiles.Items.Clear();
            checkindependency.Items.Clear();

            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("path", "../Storage");
            msg.add("command", "getFiles");
            translater.postMessage(msg);
            DirectoryInfo folder = new DirectoryInfo("../../../../clientstore");
            Console.WriteLine("\n---------------send a message to get the file list,command is getFiles---------------------");
            Console.WriteLine("\n send getFiles message from " + msg.value("from") + " to " + msg.value("to"));

            foreach (FileInfo file in folder.GetFiles("*.*"))
            {
                checkinfiles.Items.Add(file);
            }
        }

        private void initshowfilelist()
        {
            checkinfiles.Items.Clear();

            DirectoryInfo folder = new DirectoryInfo("../../../../clientstore"); 

            foreach (FileInfo file in folder.GetFiles("*.*"))
            {
                checkinfiles.Items.Add(file);

            }
        }
        private void checkout_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("\n -----------------you click the check out button--------------------------");
            if (checkoutfiles.SelectedItem != null)
            {
                CsEndPoint serverEndPoint = new CsEndPoint();
                serverEndPoint.machineAddress = "localhost";
                serverEndPoint.port = 8080;
                CsMessage msg = new CsMessage();
                msg.add("to", CsEndPoint.toString(serverEndPoint));
                msg.add("from", CsEndPoint.toString(endPoint_));
                msg.add("requestfile", checkoutfiles.SelectedItem.ToString());//checkoutfiles.SelectedItem.ToString()
                msg.add("command", "checkout");
                Console.WriteLine("\n send check out message from " + msg.value("from") + " to " + msg.value("to"));
                Console.WriteLine("\n download file from " + msg.value("to") + " to " + msg.value("from"));
                Console.WriteLine("\n file name is:");
                Console.WriteLine(checkoutfiles.SelectedItem.ToString());
                translater.postMessage(msg);
            }
            else { return; }

        }
        private void showrepo_Click(object sender, RoutedEventArgs e)
        {
            checkoutfiles.Items.Clear();
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("path", "../Storage");
            msg.add("command", "getFiles");
            Console.WriteLine("\n---------------send a message to get the file list,command is getFiles---------------------");
            Console.WriteLine("\n send getFiles message from " + msg.value("from") + " to " + msg.value("to"));
            translater.postMessage(msg);
        }


        private void viewfiles_Click(object sender, RoutedEventArgs e)
        {


            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("path", "../Storage");
            msg.add("command", "getFiles");
            Console.WriteLine("\n---------------send a message to get the file list,command is getFiles---------------------");
            Console.WriteLine("\n send getFiles message from " + msg.value("from") + " to " + msg.value("to"));
            translater.postMessage(msg);

        }

        private void search_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("\n------------------you click the search button-----------------------");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getQLists");
            msg.add("wanted", query.Text);
            Console.WriteLine("\n send search message from " + msg.value("from") + " to " + msg.value("to"));
            Console.WriteLine("\n text you want to view is:");
            Console.WriteLine(query.Text);
            translater.postMessage(msg);
        }



        private void browse_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("------------you double click the file you want to viewmetadata--------------");
            if (browsequery.SelectedItem != null)
            {
                CsEndPoint serverEndPoint = new CsEndPoint();
                serverEndPoint.machineAddress = "localhost";
                serverEndPoint.port = 8080;
                CsMessage msg = new CsMessage();
                msg.add("to", CsEndPoint.toString(serverEndPoint));
                msg.add("from", CsEndPoint.toString(endPoint_));
                msg.add("browsefile", browsequery.SelectedItem.ToString());
                msg.add("command", "viewmetadata");
                Console.WriteLine("\n send viewmetadata message from " + msg.value("from") + " to " + msg.value("to"));
                Console.WriteLine("\n file you want to view is:");
                Console.WriteLine(browsequery.SelectedItem.ToString());
                translater.postMessage(msg);
            }
        }


        private void showfilesnotclosed_Click(object sender, RoutedEventArgs e)
        {
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "showopen");
            translater.postMessage(msg);
            msg.remove("command");
            msg.add("command", "getFiles");
            translater.postMessage(msg);
        }

        private void closestatus_Click(object sender, RoutedEventArgs e)
        {
            if (notclosedfiles.SelectedItem != null)
            {
                CsEndPoint serverEndPoint = new CsEndPoint();
                serverEndPoint.machineAddress = "localhost";
                serverEndPoint.port = 8080;
                CsMessage msg = new CsMessage();
                msg.add("to", CsEndPoint.toString(serverEndPoint));
                msg.add("from", CsEndPoint.toString(endPoint_));
                msg.add("operatefile", notclosedfiles.SelectedItem.ToString());
                msg.add("author", authorname.Text);
                msg.add("command", "close");
                Console.WriteLine("\n---------------send a message to close a check in status,command is close---------------------");
                Console.WriteLine("\n send close message from " + msg.value("from") + " to " + msg.value("to"));
                translater.postMessage(msg);
            }
        }

        private void modify_Click(object sender, RoutedEventArgs e)
        {
            if (notclosedfiles.SelectedItem != null)
            {
                CsEndPoint serverEndPoint = new CsEndPoint();
                serverEndPoint.machineAddress = "localhost";
                serverEndPoint.port = 8080;
                CsMessage msg = new CsMessage();
                msg.add("to", CsEndPoint.toString(serverEndPoint));
                msg.add("from", CsEndPoint.toString(endPoint_));
                msg.add("command", "modify");
                msg.add("operatefile", notclosedfiles.SelectedItem.ToString());
                msg.add("author", authorname.Text);
                msg.add("catagary", catagary.Text);
                msg.add("descript", description.Text);
                int tempc = 0;
                foreach (var item in adddependencies.SelectedItems)
                {

                    msg.add("children" + tempc.ToString(), item.ToString());
                    tempc++;
                }
                Console.WriteLine("\n---------------send a message to modify metadata,command is modify--------------------");
                Console.WriteLine("\n send modify message from " + msg.value("from") + " to " + msg.value("to"));
                translater.postMessage(msg);
            }
        }

        private void filewithnoparent_Click(object sender, RoutedEventArgs e)
        {
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            // msg.add("path", "../Storage");
            msg.add("command", "getQLists");
            msg.add("wanted", "withnodependency");
            translater.postMessage(msg);
        }
    }
}
