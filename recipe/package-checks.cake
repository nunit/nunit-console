//////////////////////////////////////////////////////////////////////
// SYNTAX FOR EXPRESSING CHECKS
//////////////////////////////////////////////////////////////////////

public static class Check
{
	public static void That(DirectoryPath testDirPath, IList<PackageCheck> checks)
	{
		if (checks == null)
			throw new ArgumentNullException(nameof(checks));

		bool allOK = true;

		foreach (var check in checks)
			allOK &= check.ApplyTo(testDirPath);

        if (!allOK) throw new Exception("Verification failed!");
    }
}

private static FileCheck HasFile(FilePath file) => HasFiles(new[] { file });
private static FileCheck HasFiles(params FilePath[] files) => new FileCheck(files);

private static DirectoryCheck HasDirectory(string dir) => new DirectoryCheck(dir);

//////////////////////////////////////////////////////////////////////
// PACKAGECHECK CLASS
//////////////////////////////////////////////////////////////////////

public abstract class PackageCheck
{
	protected ICakeContext _context;

	public PackageCheck()
	{
		_context = BuildSettings.Context;
	}
	
	public abstract bool ApplyTo(DirectoryPath testDirPath);

	protected bool CheckDirectoryExists(DirectoryPath dirPath)
	{
		if (!_context.DirectoryExists(dirPath))
		{
			DisplayError($"Directory {dirPath} was not found.");
			return false;
		}

		return true;
	}

	protected bool CheckFileExists(FilePath filePath)
	{
		if (!_context.FileExists(filePath))
		{
			DisplayError($"File {filePath} was not found.");
			return false;
		}

		return true;
	}

	protected bool CheckFilesExist(IEnumerable<FilePath> filePaths)
	{
		bool isOK = true;

		foreach (var filePath in filePaths)
			isOK &= CheckFileExists(filePath);

		return isOK;
	}

	protected bool DisplayError(string msg)
	{
		_context.Error("  ERROR: " + msg);

		// The return value may be ignored or used as a shortcut
		// for an immediate return from ApplyTo as in
		//    return DisplayError(...)
		return false;
	}
}

//////////////////////////////////////////////////////////////////////
// FILECHECK CLASS
//////////////////////////////////////////////////////////////////////

public class FileCheck : PackageCheck
{
	FilePath[] _files;

	public FileCheck(FilePath[] files)
	{
		_files = files;
	}

	public override bool ApplyTo(DirectoryPath testDirPath)
	{
		return CheckFilesExist(_files.Select(file => testDirPath.CombineWithFilePath(file)));
	}
}

//////////////////////////////////////////////////////////////////////
// DIRECTORYCHECK CLASS
//////////////////////////////////////////////////////////////////////

public class DirectoryCheck : PackageCheck
{
	private DirectoryPath _relDirPath;
	private List<FilePath> _files = new List<FilePath>();

	public DirectoryCheck(DirectoryPath relDirPath)
	{
		_relDirPath = relDirPath;
	}

	public DirectoryCheck WithFiles(params FilePath[] files)
	{
		_files.AddRange(files);
		return this;
	}

    public DirectoryCheck AndFiles(params FilePath[] files)
    {
        return WithFiles(files);
    }

	public DirectoryCheck WithFile(FilePath file)
	{
		_files.Add(file);
		return this;
	}

    public DirectoryCheck AndFile(FilePath file)
    {
        return AndFiles(file);
    }

	public override bool ApplyTo(DirectoryPath testDirPath)
	{
		DirectoryPath absDirPath = testDirPath.Combine(_relDirPath);

		if (!CheckDirectoryExists(absDirPath))
			return false;

		return CheckFilesExist(_files.Select(file => absDirPath.CombineWithFilePath(file)));
	}
}
