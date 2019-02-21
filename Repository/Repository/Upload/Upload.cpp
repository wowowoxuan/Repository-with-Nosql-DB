/////////////////////////////////////////////////////////////////////
// checkin.cpp - Implements checkin options for repository core    //
// Weiheng Chai, CSE687 - Object Oriented Design, Spring 2018      //
/////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* -------------------
* implement changefilename and uploaddb

* Required Files:
* ---------------
* upload.h
*/
#include<iostream>
#include"Upload.h"




void Repositorycore::upload::changefilename(std::string oldname,std::string newname) {
	std::string old = "../Storage/" + oldname;
	std::string newn = "../Storage/" + newname;
	if (!rename(old.c_str(), newn.c_str())) {
		std::cout << "modify version number successful";
	};
}

std::string Repositorycore::upload::uploaddb(NoSqlDb::DbCore<NoSqlDb::PayLoad> &db, std::unordered_map<std::string, int> &version, std::string filename, std::string ns,std::string author) {
	Repositorycore::version ver;
	int v = ver.ckeckversion(version, filename);
	int v1=0;
	std::string index = ns + "-" + filename + "." + std::to_string(v);//check version
	if (v == 0 || db.dbStore()[index].payLoad().status() == "closed") {//check status
		if (v != 0 && author != db.dbStore()[index].payLoad().author()) {
			return "authorfalse";
		}
		ver.updateversion(version, filename, v);
	    v1 = ver.ckeckversion(version, filename);
		changefilename(filename,ns+"-"+ filename + "."+std::to_string(v1));
		std::cout << "\nthe new version number is :" << v1;
		NoSqlDb::PayLoad pload;
				//add dbelement
		pload.author(author);
		pload.filename(filename);
		pload.status() = "open";
		pload.value("../repostorage/" + filename + "." + std::to_string(v + 1));
		NoSqlDb::DbElement<NoSqlDb::PayLoad> elem;
		elem.name(filename);
		elem.dateTime(DateTime().now());
		elem.descrip(filename + " description");
		elem.payLoad(pload);
		db[ns + "-" + filename + "." + std::to_string(v + 1)] = elem;
	}
	else {
		std::cout << "check in is not closed, you can modify the metadata of index " + index << std::endl;//if status is open,can modify the file without change version number
		return "open";
	}
	return ns + "-" + filename + "." + std::to_string(v1);
}

#ifdef Test_cin



int main() {
	Repositorycore::upload up;
	up.changefilename("test.txt", "test.txt.1");
	getchar();
	getchar();
	return 0;

}
#endif