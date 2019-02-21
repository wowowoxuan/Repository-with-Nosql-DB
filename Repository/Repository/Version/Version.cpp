//version.cpp - implent the two functions in version.h//
//author:Weiheng Chai                                 //
////////////////////////////////////////////////////////
//required files
//version.h
//
//
#include<iostream>
#include"version.h"



int  Repositorycore::version::ckeckversion(std::unordered_map<std::string, int> map, std::string filename) {
	std::unordered_map<std::string, int>::iterator it = map.begin();
	map.find(filename);
	if (it == map.end()) {
		return 0;
	}
	if (it != map.end()) {
		return map[filename];
	}
	return 0;
}

void  Repositorycore::version::updateversion(std::unordered_map<std::string, int> &map, std::string filename, int i) {
	map[filename] = i + 1;
	std::cout << map[filename];
}

#ifdef Test_version
int main() {
	unordered_map<string, int> map;
	//map["stupid"] = 2;
	int v = version::ckeckversion(map, "stupid");
	cout << v << endl;
	version::updateversion(map, "stupid", v);
	cout << map["stupid"] << endl;
	int v2 = version::ckeckversion(map, "stupid");
	cout << v2 << endl;
	version::updateversion(map, "stupid", v2);
	cout << map["stupid"];
	getchar();
	getchar();
	return 0;


}
#endif // Test_version
