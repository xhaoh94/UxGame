set GEN_CLIENT=dotnet .\Tools\Luban.ClientServer\Luban.ClientServer.dll

%GEN_CLIENT% -j cfg --^
 --define_file Defines\__root__.xml ^
 --input_data_dir Datas ^
 --output_data_dir Client/Gen/ConfigData ^
 --output_code_dir Client/Gen/Config ^
 --gen_types code_cs_unity_json,data_json ^
 --service client
pause