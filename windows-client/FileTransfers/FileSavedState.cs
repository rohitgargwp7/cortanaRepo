using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfers
{
   public class FileSavedState 
{
	
	private static long serialVersionUID = 1L;

	private FileTransferBase.FTState _currentState;

	private int _totalSize; // (in bytes)

	private int _transferredSize;

	public FileSavedState(FileTransferBase.FTState state, int totalSize, int transferredSize)
	{
		_currentState = state;
		_totalSize = totalSize;
		_transferredSize = transferredSize;
	}

	public int getTotalSize()
	{
		return _totalSize;
	}

	public int getTransferredSize()
	{
		return _transferredSize;
	}

	public FileTransferBase.FTState getFTState()
	{
		return _currentState;
	}
}
}
