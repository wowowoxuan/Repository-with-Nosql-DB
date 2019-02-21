#pragma once
/////////////////////////////////////////////////////////////////////
// version.h - Implements version options for repository core      //
// Weiheng Chai, CSE687 - Object Oriented Design, Spring 2018      //
/////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* -------------------
* This package provides one class:
* - version implements check version and update version
* - checkverion: check the version number in the vector
* - updateversion: update the version number in the vector

* Required Files:
* ---------------
* version.h,version.cpp
* Maintenance History:
* --------------------
* ver 1.0 : 6/3 2018
* - first release
*/

#include<iostream>
#include <unordered_map>

//using namespace std;
namespace Repositorycore {
	class version {
	public:
		int ckeckversion(std::unordered_map<std::string, int> map, std::string filename);//check the most up-to-date version number,stored version information in a unordered map in repocore.

		void updateversion(std::unordered_map<std::string, int> &map, std::string filename, int i);//when check in a file that its previous version is closed, do update version
	};
}
