1. Use IE 10 (what about IE9?)
2. (not required) Tick "Allow active content to run in files on My Computer" setting of IE (Options -> Advanced -> Security) 
3. Add following DWORD keys (works beginning from Internet Explore 8). Actually, the value should point to the version 
of the IE to use by WebBrowser control (9000 for IE9) but it looks like zero value means the installed browser:
- key=PronunciationTrainer.exe, value=0
- key=PronunciationTrainer.vshost.exe, value=0
to
"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION" (for 32 bit)
or
"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION" (for 64 bit)

See http://www.cnblogs.com/philzhou/archive/2012/12/02/2798204.html for details.