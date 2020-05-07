
@cd ./LocalProxy
@start node index.js

@echo set proxy 127.0.0.1:8000
@echo login aigis
@pause

@start AigisTools.bat

@cd ../AigisTools
@pause
@start get_icondata.bat
@pause
@start AigisChecker.bat
@pause

@cd ../AigisChecker
@start node index.js

@echo done!!
@pause