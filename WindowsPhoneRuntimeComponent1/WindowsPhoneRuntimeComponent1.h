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
		static void myfunc(uint8 position, Platform::WriteOnlyArray<uint8>^ intOutArray, Platform::String^* strVideoFilePath,Platform::String^* strVideoFilename);
		static uint16 GetVideoCount();
	};
}