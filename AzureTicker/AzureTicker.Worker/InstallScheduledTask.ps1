function Get-Script-Directory
{
    $scriptInvocation = (Get-Variable MyInvocation -Scope 1).Value
    return Split-Path $scriptInvocation.MyCommand.Path
}

function Translate($path, $value)
{
	$configXmlPath = "$path\Run\SeleniumTest.exe.config";
	[System.Xml.XmlDocument] $xdoc = new-object System.Xml.XmlDocument;
	$xdoc.load($configXmlPath);

	$parentNode = $xdoc.SelectSingleNode("/configuration/applicationSettings/SeleniumTest.Properties.Settings");
	$thumb1 = $parentNode.ChildNodes.Item(1).FirstChild.get_InnerXML();
	$thumb2 = $parentNode.ChildNodes.Item(2).FirstChild.get_InnerXML();

	$typePath = "$path\Run\TickerEncryption35.dll";
	Add-Type -Path $typePath
	$output = [TickerEncryption35.Utility]::Decrypt($value, $thumb1, $thumb2);
	return $output
} 

$basePath = $(Get-Script-Directory);
$translatedValue = Translate $basePath ""
$combinedXmlPath = "$(Get-Script-Directory)\AzureTicker_Notification_Task.xml";
#$commandPath = "$(Get-Script-Directory)\Run\SeleniumTest.exe";
$commandPath = "D:\run\SeleniumTest.exe";
$computername = [System.Net.Dns]::GetHostName();
$currentDate = $(get-date);
$currentDateString = [string]::format("{0}-{1}-{2}T{3}:{4}:{5}.{6}", $currentDate.Year, $currentDate.Month.ToString("D2"), $currentDate.Day.ToString("D2"), $currentDate.Hour.ToString("D2"), $currentDate.Minute.ToString("D2"), $currentDate.Second.ToString("D2"), $currentDate.Millisecond);
$username = $args[0];
$fullusername = [string]::format("{0}\{1}", $computername, $username);
$intervalValue = "PT10M";
$actionString = "UpdateBalance";

[System.Xml.XmlDocument] $xd = new-object System.Xml.XmlDocument;
$xd.load($combinedXmlPath);

# Create an XmlNamespaceManager for resolving namespaces 
$nsmgr = New-Object System.Xml.XmlNamespaceManager($xd.NameTable); 
$nsmgr.AddNamespace("ns1", "http://schemas.microsoft.com/windows/2004/02/mit/task");

#Set RegistrationInfo Date node to be current date
$registrationDateNode = $xd.SelectSingleNode("/ns1:Task/ns1:RegistrationInfo/ns1:Date", $nsmgr);
#Write-Host $registrationDateNode.get_InnerXML();
$registrationDateNode.set_InnerXml($currentDateString)

# Set RegistrationInfo Author node to be current user
$authornode = $xd.SelectSingleNode("/ns1:Task/ns1:RegistrationInfo/ns1:Author", $nsmgr);
#Write-Host $authornode.get_InnerXML();
$authornode.set_InnerXml($fullusername);

# Set TimeTrigger - Start Boundary node to be current datetime
$startboundarynode = $xd.SelectSingleNode("/ns1:Task/ns1:Triggers/ns1:TimeTrigger/ns1:StartBoundary", $nsmgr);
#Write-Host $startboundarynode.get_InnerXML();
$startboundarynode.set_InnerXml($currentDateString);

# Set TimeTrigger - Repetition - Interval node to be 10 mins
$intervalnode = $xd.SelectSingleNode("/ns1:Task/ns1:Triggers/ns1:TimeTrigger/ns1:Repetition/ns1:Interval", $nsmgr);
$intervalnode.set_InnerXml($intervalValue);

# Set UserId Principal - to be current current user
$userIdnode = $xd.SelectSingleNode("/ns1:Task/ns1:Principals/ns1:Principal/ns1:UserId", $nsmgr);
#Write-Host $userIdnode.get_InnerXML();
$userIdnode.set_InnerXml($fullusername);

# Set Action - Command Node value - to be current location of SeleniumTest
$commandnode = $xd.SelectSingleNode("/ns1:Task/ns1:Actions/ns1:Exec/ns1:Command", $nsmgr);
#Write-Host $commandnode.get_InnerXML();
$commandnode.set_InnerXml($commandPath);

# Set Action - Arguments Node value - to be current action of SeleniumTest
$argumentnode = $xd.SelectSingleNode("/ns1:Task/ns1:Actions/ns1:Exec/ns1:Arguments", $nsmgr);
$argumentnode.set_InnerXml($actionString);

$xd.Save($combinedXmlPath);

& schtasks.exe /create /RU $fullusername /RP $translatedValue /TN AzureTickerBalanceUpdater /XML $combinedXmlPath

$actionString = "SendNotifications";
$argumentnode.set_InnerXml($actionString);

$intervalValue = "PT5M";
$intervalnode.set_InnerXml($intervalValue);
$xd.Save($combinedXmlPath);

& schtasks.exe /create /RU $fullusername /RP $translatedValue /TN AzureTickerNotificationSender /XML $combinedXmlPath 