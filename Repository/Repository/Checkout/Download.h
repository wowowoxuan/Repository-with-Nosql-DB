#pragma once

/////////////////////////////////////////////////////////////////////
// checkin.h - Implements checkin options for repository core      //
// Weiheng Chai, CSE687 - Object Oriented Design, Spring 2018      //
/////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* -------------------
* This package provides one class:
* - changefilename: change the name of a file
* - downloadfile:copy file from one folder to another and rename it according to the name in db
* - copyfileandrename:copy a file and rename it
* - deletefile:delete afile

* Required Files:
* ---------------
* download.h,download.cpp
*
* Maintenance History:
* --------------------
* ver 2.0 : 30/3 2018
* - first release
*/
#include<iostream>
#include"../Version/version.h"
#include"../DbCore/DbCore.h"
#include"../PayLoad/PayLoad.h"
#include<vector>
namespace Repositorycore {
	class download {
	public:

		void changefilename(std::string oldname, std::string newname);
		std::string downloadfile(NoSqlDb::DbCore<NoSqlDb::PayLoad> &db, std::string filename);
		bool copyfileandrename(std::string oldname, std::string newname);
		bool deletefile(std::string filename);
	};



}