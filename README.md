# Nodify_LYLCPro
基于Nodify WPF库创建的模块化低代码插件平台(MVVM模式).适用于上位机低代码平台的开发.
存在使用商用库.如介意,敬请期待使用开源库重构
## 使用以下库
          HslCommunication 12.9.1          
          MahApps.Metro 2.4.11
          Nodify 7.3.0
          OpcUaHelper 2.2.1                
          System.IO.Ports 10.0.10 
### 注： 
    1.如要长时间测试除OPC UA协议,就需要向  http://www.hsltechnology.cn/  购买HslCommunication库的激活码;
    2.需在net9.0-windows下运行(若无,需前往[官网下载SDK](https://dotnet.microsoft.com/zh-cn/download) ,或者在PowerShell中执行 winget install Microsoft.DotNet.SDK.9 );
    3.如需了解Nodify详细代码,请前往 [Nodify](https://github.com/miroiu/nodify) 仓库下载Nodify;
    4.如需要使用Python脚本插件,需要提前安装Python 3.6+,并且须在系统PATH中添加Python的安装路径;
    5.当使用HTTPServer协议时,需要外部访问,需要以管理员身份运行可执行文件或者管理员身份运行或执行：
         netsh http add urlacl url = http://+:8080/ user=Everyone,其中8080换成实际端口即可;

## 通过测试的通讯方式：
✔ OPC UA通讯
✔ HTTP通讯
✔ TCPClient/Server


### UI显示画面 
运行后的UI界面为（已编译通过）：
<img width="1920" height="1033" alt="image" src="https://github.com/user-attachments/assets/19bfa7b8-97ac-40c3-95f1-afa278a4a2fd" />
测试OPC UA 协议通讯读取写入的示意：
<img width="1920" height="1036" alt="image" src="https://github.com/user-attachments/assets/4d4b0edb-2eca-4db2-a4bf-9a1158f5e9cb" />
测试Python脚本、表达式的示意：
<img width="1920" height="1039" alt="image" src="https://github.com/user-attachments/assets/5992d8a9-bc3f-414d-886d-1601b9273656" />

## 联系方式：
邮箱:huyi1022@vip.qq.com          
微信:huyi1022

### DeBug：
当出现Bug或者代码有那些需要优化的情况,欢迎提 Issue、PR，一起完善。

## 若感觉不错(可支持一杯☕️),可点击以下Star⭐：
<img width="605" height="312" alt="image" src="https://github.com/user-attachments/assets/50f75c99-0280-4456-825f-7991a8b8f57d" />

### 后续暂定计划：
          1.增加UI设计页,将Nodify节点得到的数据进行绑定;使设计后的UI成为软件主界面.
          2.增加通讯配置页面,在Nodify通讯类的插件模块中，增加可选择已完成配置的链接;
          3.因HslCommunication库存在收费问题,后续会重新用SNet库重构通讯库,并重建一个新仓库存储;










