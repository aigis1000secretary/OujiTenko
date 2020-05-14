
@cd ./LocalProxy
@echo set proxy 127.0.0.1:8000 and login Aigis
@node index.js

@pause

@echo moveing data...
@start AigisTools.bat
@pause

@echo do get_icondata.bat
@cd ../AigisTools
@start get_icondata.bat
@pause

@echo moveing data...
@start AigisChecker.bat
@pause

@echo making checker data
@cd ../AigisChecker
@node index.js

@echo done!!
@pause