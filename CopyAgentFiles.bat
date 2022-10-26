:: As each target runtime build is completed, we copy the agent files to
:: both the console and engine output directories, so they are available
:: for running tests and packaging. Supporting assemblies are copied too.
:: This batch filecopies agent files from one location to another as
:: specified by the two arguments

set SOURCE_FILES=%1
set TARGET_DIR=%2

echo Copying agent files
echo  from %SOURCE_FILES%
echo    to %TARGET_DIR%
xcopy %SOURCE_FILES% %TARGET_DIR% /S /Y /Q

exit 0