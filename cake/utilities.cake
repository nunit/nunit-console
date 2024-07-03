//////////////////////////////////////////////////////////////////////
// HELPER METHODS - PACKAGING
//////////////////////////////////////////////////////////////////////

public void CopyPackageContents(DirectoryPath packageDir, DirectoryPath outDir)
{
    var files = GetFiles(packageDir + "/tools/*").Concat(GetFiles(packageDir + "/tools/net462/*"));
    CopyFiles(files.Where(f => f.GetExtension() != ".addins"), outDir);
}

