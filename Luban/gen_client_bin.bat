set GEN_CLIENT=dotnet .\Tools\Luban.ClientServer\Luban.ClientServer.dll
set CODE_DIR=..\Unity\Assets\Hotfix\CodeGen\Config
set DATA_DIR=..\Unity\Assets\Data\Res\Config

%GEN_CLIENT% -j cfg --^
 --define_file Defines\__root__.xml ^
 --input_data_dir Datas ^
 --output_data_dir %DATA_DIR% ^
 --output_code_dir %CODE_DIR% ^
 --gen_types code_cs_unity_bin,data_bin ^
 --service client
pause