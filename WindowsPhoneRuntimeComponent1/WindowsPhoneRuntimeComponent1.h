#pragma once
#include <vector>
#include "zmedialib.h"
using namespace Platform;
using namespace std;
using namespace Windows::Foundation::Collections;

namespace WindowsPhoneRuntimeComponent1
{

	public ref class WindowsPhoneRuntimeComponent sealed
	{
		ZMEDIAITEM *rgItemsRoot;
	public:
		WindowsPhoneRuntimeComponent();
		Platform::Array<uint8>^ GetVideoInfo(uint8 position, Platform::String^* strVideoFilePath,Platform::String^* strVideoFilename,float64* videoDate);
		uint16 GetVideoCount();
		void ClearData();
	};
}