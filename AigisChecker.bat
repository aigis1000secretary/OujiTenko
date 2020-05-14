
@cd ./LocalProxy
@echo set proxy 127.0.0.1:8000
@echo login aigis
@node index.js

@pause

@start AigisTools.bat

@cd ../AigisTools
@pause
@start get_icondata.bat
@pause
@start AigisChecker.bat
@pause

@cd ../AigisChecker
@node index.js

@echo done!!
@pause