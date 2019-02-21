#pragma once
///////////////////////////////////////////////////////////////////////
// ServerPrototype.h - Console App that processes incoming messages  //
// ver 1.0                                                           //
// author Weiheng Chai                                               //
// source Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018  //
///////////////////////////////////////////////////////////////////////
/*
*  Package Operations:
* ---------------------
*  Package contains one class, Server, that contains a Message-Passing Communication
*  facility. It processes each message by invoking an installed callable object
*  defined by the message's command key.
*
*  Message handling runs on a child thread, so the Server main thread is free to do
*  any necessary background processing (none, so far).
*
*  Required Files:
* -----------------
*  ServerPrototype.h, ServerPrototype.cpp
*  Comm.h, Comm.cpp, IComm.h
*  Message.h, Message.cpp
*  FileSystem.h, FileSystem.cpp
*  Utilities.h
*
*  Maintenance History:
* ----------------------
*  ver 1.0 : 3/27/2018
*  - first release
*/
#include <vector>
#include <string>
#include <unordered_map>
#include <functional>
#include <thread>
#include "../CppCommWithFileXfer/Message/Message.h"
#include "../CppCommWithFileXfer/MsgPassingComm/Comm.h"
#include <windows.h>
#include <tchar.h>
#include "../DbCore/DbCore.h"
#include "../PayLoad/PayLoad.h"
#include "../Upload/Upload.h"
#include "../Checkout/Download.h"
#include "../Query/Query.h"

namespace Repository
{
  using File = std::string;
  using Files = std::vector<File>;
  using Dir = std::string;
  using Dirs = std::vector<Dir>;
  using SearchPath = std::string;
  using Key = std::string;
  using Msg = MsgPassingCommunication::Message;
  using ServerProc = std::function<Msg(Msg,NoSqlDb::DbCore<NoSqlDb::PayLoad>&,std::unordered_map<std::string,int>&)>;
  using MsgDispatcher = std::unordered_map<Key,ServerProc>;
  
  const SearchPath storageRoot = "../Storage";  // root for all server file storage
  const MsgPassingCommunication::EndPoint serverEndPoint("localhost", 8080);  // listening endpoint

  class Server
  {
  public:
    Server(MsgPassingCommunication::EndPoint ep, const std::string& name);
    void start();
    void stop();
    void addMsgProc(Key key, ServerProc proc);
    void processMessages();
    void postMessage(MsgPassingCommunication::Message msg);
    MsgPassingCommunication::Message getMessage();
    static Dirs getDirs(const SearchPath& path = storageRoot);
    static Files getFiles(const SearchPath& path = storageRoot);
	/*get set*/
	std::unordered_map<std::string, int>& version() { return version_; }
	std::unordered_map<std::string, int> version() const { return version_; }
	void version(const std::unordered_map<std::string, int>& version) { version_ = version; }

	NoSqlDb::DbCore<NoSqlDb::PayLoad>& DbCore1() { return DbCore1_; }
	NoSqlDb::DbCore<NoSqlDb::PayLoad> DbCore1() const { return DbCore1_; }
	void DbCore1(const NoSqlDb::DbCore<NoSqlDb::PayLoad>& DbCore) { DbCore1_ = DbCore; }

	std::vector<std::string>& checked() { return checked_; }
	std::vector<std::string> checked() const { return checked_; }
	void checked(const std::vector<std::string>& checked) { checked_ = checked; }
  private:
    MsgPassingCommunication::Comm comm_;
    MsgDispatcher dispatcher_;
    std::thread msgProcThrd_;
	/*nosqldb and version management*/
	NoSqlDb::DbCore<NoSqlDb::PayLoad> DbCore1_;
	std::unordered_map<std::string, int> version_;//used for manage version number
	std::vector<std::string> checked_;
  };
  //----< initialize server endpoint and give server a name >----------
  inline Server::Server(MsgPassingCommunication::EndPoint ep, const std::string& name)
    : comm_(ep, name) {}

  //----< start server's instance of Comm >----------------------------

  inline void Server::start()
  {
    comm_.start();
  }
  //----< stop Comm instance >-----------------------------------------

  inline void Server::stop()
  {
    if(msgProcThrd_.joinable())
      msgProcThrd_.join();
    comm_.stop();
  }
  //----< pass message to Comm for sending >---------------------------

  inline void Server::postMessage(MsgPassingCommunication::Message msg)
  {
    comm_.postMessage(msg);
  }
  //----< get message from Comm >--------------------------------------

  inline MsgPassingCommunication::Message Server::getMessage()
  {
    Msg msg = comm_.getMessage();
    return msg;
  }
  //----< add ServerProc callable object to server's dispatcher >------

  inline void Server::addMsgProc(Key key, ServerProc proc)
  {
    dispatcher_[key] = proc;
  }
  //----< start processing messages on child thread >------------------

  inline void Server::processMessages()
  {
    auto proc = [&]()
    {
      if (dispatcher_.size() == 0)
      {
        std::cout << "\n  no server procs to call";
        return;
      }
      while (true)
	  {		  try {
			  Msg msg = getMessage();
			  if (msg.attributes().size() == 0) continue;
			  std::cout << "\n  received message: " << msg.command() << " from " << msg.from().toString();
			  if (msg.command() == "serverQuit")
				  break;
			  Msg reply = dispatcher_[msg.command()](msg, DbCore1(), version());
			  if (msg.command() == "checkout") {//here send all dependencies to the client
				  for (auto key : reply.keys()) {
					  if (key.find("depencies") != -1) {
						  Msg msgtemp;
						  msgtemp.to(msg.from());
						  msgtemp.from(msg.to());
						  msgtemp.command("depencyfile");
						  msgtemp.attribute("file", reply.attributes()[key]);
						  msgtemp.attribute("sendpath", "../tempfile");
						  msgtemp.attribute("receivepath", "../../../../clientstore");
						  postMessage(msgtemp);
					  }
				  }
			  }
			  if (msg.to().port != msg.from().port)  // avoid infinite message loop
			  {
				  postMessage(reply);
				  msg.show();
				  reply.show();
			  }
			  else
				  std::cout << "\n  server attempting to post to self";
		  }
	  catch (const std::exception& e) {  }
	  }
      std::cout << "\n  server message processing thread is shutting down";
    };
    std::thread t(proc);
    std::cout << "\n  starting server thread to process messages";
    msgProcThrd_ = std::move(t);
  }

}