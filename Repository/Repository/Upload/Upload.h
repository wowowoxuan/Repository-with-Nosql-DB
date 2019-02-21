#pragma once
/////////////////////////////////////////////////////////////////////
// checkin.h - Implements checkin options for repository core      //
// Weiheng Chai, CSE687 - Object Oriented Design, Spring 2018      //
/////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* -------------------
* This package provides one class:
* - changefilename:change the file name
* - uploaddb: add new element to the dbcore

* Required Files:
* ---------------
* upload.h,upload.cpp
* 
* Maintenance History:
* --------------------
* ver 2.0 : 28/4 2018
* - first release
*/
#include<iostream>
#include"../Version/version.h"
#include"../DbCore/DbCore.h"
#include"../PayLoad/PayLoad.h"
namespace Repositorycore {
	class upload  {
	public:
		
		void changefilename(std::string oldname,std::string newname);
	    std::string uploaddb(NoSqlDb::DbCore<NoSqlDb::PayLoad> &db,std::unordered_map<std::string,int> &version,std::string filename, std::string ns,std::string author);

	};



}
