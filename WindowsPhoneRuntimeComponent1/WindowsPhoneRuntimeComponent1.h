#pragma once
#include <vector>
using namespace Platform;
using namespace std;
using namespace Windows::Foundation::Collections;

namespace WindowsPhoneRuntimeComponent1
{

	public ref class WindowsPhoneRuntimeComponent sealed
	{
	public:
		WindowsPhoneRuntimeComponent();
		static Platform::Array<uint8>^ myfunc(uint8 position, Platform::String^* strVideoFilePath,Platform::String^* strVideoFilename,Platform::String^* strVideoAlbumname);
		static uint16 GetVideoCount();
	};
}