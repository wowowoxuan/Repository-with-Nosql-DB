/////////////////////////////////////////////////////////////////////////
// ServerPrototype.cpp - Console App that processes incoming messages  //
// ver 1.0                                                             //
// author Weiheng Chai                                                 //  
// source Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018    //
/////////////////////////////////////////////////////////////////////////

#include "ServerPrototype.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include <chrono>

namespace MsgPassComm = MsgPassingCommunication;

using namespace Repository;
using namespace FileSystem;
using Msg = MsgPassingCommunication::Message;
using namespace NoSqlDb;



Files Server::getFiles(const Repository::SearchPath& path)
{
  return Directory::getFiles(path);
}

Dirs Server::getDirs(const Repository::SearchPath& path)
{
  return Directory::getDirectories(path);
}

template<typename T>
void show(const T& t, const std::string& msg)
{
  std::cout << "\n  " << msg.c_str();
  for (auto item : t)
  {
    std::cout << "\n    " << item.c_str();
  }
}

std::function<Msg(Msg,DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> echo = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
  Msg reply = msg;
  reply.to(msg.from());
  reply.from(msg.to());
  return reply;
};

std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> getFiles = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
  Msg reply;
  reply.to(msg.from());
  reply.from(msg.to());
  reply.command("getFiles");
  size_t t = 0;
  for (auto file : db) {
	  if (file.second.payLoad().status() != "mine") {
		  reply.attribute("file" + std::to_string(t), file.first);
		  t++;
		  std::cout << file.first;
	  }
  }
  return reply;
};
/*def query*/
//getQLists defines what the server do when receive a query command, the command in the message named getQLists.
std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> getQLists = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
	Msg reply;
	reply.to(msg.from());
	reply.from(msg.to());
	reply.command("getQLists");
	//std::string path = msg.value("path");
	
	NoSqlDb::Query<NoSqlDb::PayLoad> q1(db);
	NoSqlDb::Conditions<NoSqlDb::PayLoad> c1;
	if (msg.attributes()["wanted"] == "withnodependency") {//send the list of files that has no parent.
		size_t t = 0;
		for (auto file : db) {
			if (file.second.ischildren()=="false") {
				reply.attribute("file" + std::to_string(t), file.first);
				t++;
				std::cout << file.first;
			}
		}
		return reply;
	}
	c1.name(msg.attributes()["wanted"]);//else query the name according to the key word input in the browse tab.

	size_t count = 0;
		for (auto item : q1.select(c1).keys())
		{
			std::string countStr = Utilities::Converter<size_t>::toString(++count);
			reply.attribute("file" + countStr, item);
		}

	return reply;
};
/*def show open*/
//send all the files in a list, which checkin is open.
std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> showopen = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
	Msg reply;
	reply.to(msg.from());
	reply.from(msg.to());
	reply.command("showopen");
	size_t t = 0;
	for (auto file : db) {
		if (file.second.payLoad().status() != "closed") {
			reply.attribute("statusopen" + std::to_string(t), file.first);
			t++;
			std::cout << file.first;
	  }
	}
	return reply;
};
/*def show close*/
//show the files which are checkin closed. Did not used now, but I think it may be used, so I added it here.
std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> showclose = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
	Msg reply;
	reply.to(msg.from());
	reply.from(msg.to());
	reply.command("showclose");
	size_t t = 0;
	for (auto file : db) {
		if (file.second.payLoad().status() != "closed") {
			reply.attribute("statusclose" + std::to_string(t), file.first);
			t++;
			std::cout << file.first;
		}
	}
	return reply;
};

/*def close*/
//close a file's checkin
std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> close = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
	Msg reply;
	reply.to(msg.from());
	reply.from(msg.to());
	reply.command("close");
	if (msg.attributes()["author"] != db[msg.attributes()["operatefile"]].payLoad().author()) {
		reply.attribute("result", "false");
		return reply;
	}
	db[msg.attributes()["operatefile"]].payLoad().status() = "closed";
	reply.attribute("result", "true");
	return reply;
};
/*def modify*/
std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> modify = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
	Msg reply;
	reply.to(msg.from());
	reply.from(msg.to());
	reply.command("modify");
	if (msg.attributes()["author"] != db[msg.attributes()["operatefile"]].payLoad().author()) {//check if the author is right
		reply.attribute("result", "false");
		return reply;
	}
	db[msg.attributes()["operatefile"]].descrip(msg.attributes()["descript"]);//modify description
	db[msg.attributes()["operatefile"]].payLoad().categories().push_back(msg.attributes()["catagary"]);//modify categories
	for (auto key : msg.keys()) {
	if (key.find("children")!=-1) {
		
		/*db[msg.attributes()["operatefile"]].children().push_back(msg.attributes()[key]);*/
		db[msg.attributes()["operatefile"]].addChildKey(msg.attributes()[key]);
		db[msg.attributes()[key]].ischildren("true");
	}
	}
	reply.attribute("result", "true");
	return reply;
};
/*def checkin*/
std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> checkin = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {

	Msg reply;
	reply.to(msg.from());
	reply.from(msg.to());
	reply.command("checkin");
	Repositorycore::upload upl;
	std::string stemp=upl.uploaddb(db,version,msg.attributes()["file"],msg.attributes()["namespace"],msg.attributes()["author"]);//the upl function will return the file name in the repository if you are the author and check in is closed
	if (stemp == "authorfalse") {
		reply.attribute("replycin", "authorfalse");
		return reply;
	}
	else if (stemp == "open") {
		reply.attribute("replycin", "open");
		return reply;
	}
	for (auto key : msg.keys()) {
		if (key.find("children")!=-1) {
			db[stemp].children().push_back(msg.attributes()[key]);
			db[msg.attributes()[key]].ischildren("false");
		}
	}
	db[stemp].descrip(msg.attributes()["description"]);
	db[stemp].payLoad().categories().push_back(msg.attributes()["category"]);
	db[stemp].ischildren("false");

		reply.attribute("replycin", "success");
	
	NoSqlDb::showDb(db);
	std::cout << std::endl;
	std::cout << "\n test checkin passed";
	return reply;
};
/*def view metadata*/
//send a message include metadata to the client.
std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> viewmetadata = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {

	Msg reply;
	reply.to(msg.from());
	reply.from(msg.to());
	reply.command("viewmetadata");
	reply.attributes()["bauthor"] = db[msg.attributes()["browsefile"]].payLoad().author();
	reply.attributes()["datatime"] = db[msg.attributes()["browsefile"]].dateTime();
	reply.attributes()["bfilename"] = db[msg.attributes()["browsefile"]].name();
	reply.attributes()["descript"] = db[msg.attributes()["browsefile"]].descrip();
	if (db[msg.attributes()["browsefile"]].children().size()!=0) {
		size_t temp = 0;
		for (auto file : db[msg.attributes()["browsefile"]].children()) {
			reply.attributes()["depencies"+std::to_string(temp)] = file;
			temp++;
		}
	}
	if (db[msg.attributes()["browsefile"]].payLoad().categories().size() != 0) {
		size_t temp = 0;
		for (auto file : db[msg.attributes()["browsefile"]].payLoad().categories()) {
			reply.attributes()["category" + std::to_string(temp)] = file;
			temp++;
		}
	}
	Repositorycore::download dwl;
	std::string s1 = dwl.downloadfile(db, msg.attributes()["browsefile"]);
    reply.attribute("file", s1);
    reply.attribute("sendpath", "../tempfile");
    reply.attribute("receivepath", "../../../../popupcode");
	return reply;
};


/*def checkout*/
std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> checkout = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
	std::string	s1;
	Repositorycore::download dwl;
	Msg reply;
	reply.to(msg.from());
	reply.from(msg.to());
	reply.command("checkout");
	if(msg.attributes()["requestfile"]!=""){ 
	s1 = dwl.downloadfile(db, msg.attributes()["requestfile"]);
	}
	if (db[msg.attributes()["requestfile"]].children().size() != 0) {//when process message, the server will send the requestfile one by one
		size_t temp = 0;
		for (auto file : db[msg.attributes()["requestfile"]].children()) {
			std::string s2 = dwl.downloadfile(db, file);
			reply.attributes()["depencies" + std::to_string(temp)] = s2;
			temp++;
		}
	}
	reply.attribute("file", s1);
	reply.attribute("sendpath", "../tempfile");
	reply.attribute("receivepath", "../../../../clientstore");
	return reply;
};

std::function<Msg(Msg, DbCore<PayLoad>&, std::unordered_map<std::string, int>&)> getDirs = [](Msg msg, DbCore<PayLoad>& db, std::unordered_map<std::string, int>& version) {
  Msg reply;
  reply.to(msg.from());
  reply.from(msg.to());
  reply.command("getDirs");
  std::string path = msg.value("path");
  if (path != "")
  {
    std::string searchPath = storageRoot;
    if (path != ".")
      searchPath = searchPath + "\\" + path;
    Files dirs = Server::getDirs(searchPath);
    size_t count = 0;
    for (auto item : dirs)
    {
      if (item != ".." && item != ".")
      {
        std::string countStr = Utilities::Converter<size_t>::toString(++count);
        reply.attribute("dir" + countStr, item);
      }
    }
  }
  else
  {
    std::cout << "\n  getDirs message did not define a path attribute";
  }
  return reply;
};

int main()
{
  std::cout << "\n  This is the Server";
  std::cout << "\n ==========================";
  std::cout << "\n";
  Server server(serverEndPoint, "ServerPrototype");
  std::cout << "===================================================";
  showDb(server.DbCore1());
  std::cout << "====================================================";
  server.start();  
  server.addMsgProc("echo", echo);
  server.addMsgProc("getFiles", getFiles);
  server.addMsgProc("getDirs", getDirs);
  server.addMsgProc("checkin", checkin);
  server.addMsgProc("serverQuit", echo);
  server.addMsgProc("checkout", checkout);
  server.addMsgProc("viewmetadata", viewmetadata);
  server.addMsgProc("getQLists", getQLists);
  server.addMsgProc("showopen", showopen);
  server.addMsgProc("close", close);
  server.addMsgProc("showclose", showclose);
  server.addMsgProc("modify", modify);
  server.processMessages(); 
  server.stop();
  return 0;
}

