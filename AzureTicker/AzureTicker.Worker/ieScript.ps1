$AdminKey = "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}"
$UserKey = "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}"
$IntelliFormsKey = "HKCU:\Software\Microsoft\Internet Explorer\IntelliForms"
$RunOnceKey = "HKCU:\Software\Microsoft\Internet Explorer\Main"
Set-ItemProperty -Path $AdminKey -Name "IsInstalled" -Value 0
Set-ItemProperty -Path $UserKey -Name "IsInstalled" -Value 0
Set-ItemProperty -Path $IntelliFormsKey -Name "AskUser" -Value 0
Set-ItemProperty -Path $RunOnceKey -Name "RunOnceComplete" -Value 1
Set-ItemProperty -Path $RunOnceKey -Name "RunOnceHasShown" -Value 1
