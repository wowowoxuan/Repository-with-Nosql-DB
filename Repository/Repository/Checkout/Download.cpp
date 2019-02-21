/////////////////////////////////////////////////////////////////////
// checkin.cpp - Implements checkin options for repository core    //
// Weiheng Chai, CSE687 - Object Oriented Design, Spring 2018      //
/////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* -------------------
* implement changefilename,copyfileandrename,downloadfile,deletefile

* Required Files:
* ---------------
* download.h
*/
#include<iostream>
#include<fstream>
#include"Download.h"




void Repositorycore::download::changefilename(std::string oldname, std::string newname) {
	std::string old = "../Storage/" + oldname;
	std::string newn = "../Storage/" + newname;
	if (!rename(old.c_str(), newn.c_str())) {
		std::cout << "modify version number successful";
	};
}

bool Repositorycore::download::copyfileandrename(std::string oldname,std::string newname) {
	std::ifstream in;
	std::ofstream out;
	in.open(oldname, std::ios::binary);
	if (in.fail()) {
		std::cout << "cannot open file file" << std::endl;
		out.close();
		in.close();
		return false;
	}
	out.open(newname, std::ios::binary);
	if (out.fail()) {
		std::cout << "cannot create new file" << std::endl;
		out.close();
		in.close();
		return false;
	}
	else {
		out << in.rdbuf();
		out.close();
		in.close();
		return true;
	}
	return false;
}
std::string Repositorycore::download::downloadfile(NoSqlDb::DbCore<NoSqlDb::PayLoad> &db,  std::string filename) {
	
	copyfileandrename("../Storage/" + filename, "../tempfile/"+db[filename].name());
	return db[filename].name();
	

}

bool Repositorycore::download::deletefile(std::string filename) {
	if (std::remove(filename.c_str())) {
		std::cout << "delete file success" << std::endl;
		return true;
	}
	return false;
}
#ifdef Test_cout



int main() {
	Repositorycore::download down;
	//.changefilename("test.txt", "test.txt.1");
	//down.copyfileandrename("../test/test1.txt", "../test/test2.txt");
	down.deletefile("../test/test2.txt");
	getchar();
	getchar();
	return 0;

}
#endif