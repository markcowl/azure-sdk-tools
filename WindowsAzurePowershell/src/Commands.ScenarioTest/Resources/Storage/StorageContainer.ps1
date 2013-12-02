# ----------------------------------------------------------------------------------
#
# Copyright Microsoft Corporation
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
# ----------------------------------------------------------------------------------

$containerName = "testcredentials-storage";
$containerPrefix = "testcredentials-";
$global:createdStorageAccounts = New-Object "System.Collections.Generic.List``1[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]";

<#
.SYNOPSIS
<<<<<<< HEAD
Gets valid and available storage service name.
#>
function New-StorageServiceName
{
	do
	{
		$name = "onesdk" + (Get-Random).ToString();
		$used = Test-AzureName -Storage $name;
	} while ($used)

	return $name;
}

<#
.SYNOPSIS
Gets a location that uses storage services.
#>
function Get-DefaultLocation
{
    $locations = Get-AzureLocation;
	foreach ($location in $locations)
	{
	   if ($location.AvailableServices.Contains("Storage"))
	   {
	       return $location.Name;
		}
	}

	return $null;
}


<#
.SYNOPSIS
Waits until an account is created
.PARAMETER
  The account name to check
.RETURN
  True if the account is created, otherwise false
#>
function Wait-UntilCreated
{
    param([string] $account);
    Retry-Function { Verify-AccountCreated $account} $null 30 10;
}

<#
.SYNOPSIS
Checks if an account is created
.PARAMETER
  The account name to check
.RETURN
  True if the account is created, otherwise false
#>
function Verify-AccountCreated
{
    param([string] $account);
    return ((Get-AzureStorageAccount $account).StorageAccountStatus -eq "Created");
}

<#
.SYNOPSIS
Creates a new storage service
#>
function New-StorageService
{
   $serviceName = (New-StorageServiceName);
   $location = (Get-DefaultLocation);
   $toss = New-AzureStorageAccount $serviceName -Location $location;
   $toss = Assert-True {Wait-UntilCreated $serviceName} "Unable to create storage account in the allotted time, test setup failure";
   $global:createdStorageAccounts.Add($serviceName);
   return $serviceName;
}

<#
.SYNOPSIS
Returns a storage account containing at least one container
#>
function Create-StorageAccount
{
    $storageService = New-StorageService;
	$toss = Provision-StorageCredentials $storageService;
	$toss = Create-TestContainer;
    return $storageService;
}

<#
.SYNOPSIS
Creates a test container in the current storage account
#>
function Create-TestContainer
{
   New-AzureStorageContainer $containerName;
}

<#
.SYNOPSIS
Cleans up any created containers and storage accounts
#>
function Cleanup-StorageAccounts
{
    foreach ($account in ($global:createdStorageAccounts.ToArray()))
	{
	   Provision-StorageCredentials $account;
	   try
	   {
	      foreach ($storageContainer in (Get-AzureStorageContainer))
		  {
		     Remove-AzureStorageContainer $storageContainer.Name -Force;
		  }

		  Remove-AzureStorageAccount $account;
		  $global:createdStorageAccounts.Remove($account);
	   }
	   finally 
	   {
	      Cleanup-StorageCredentials;
	   }
	}
}

<#
.SYNOPSIS
Sets up environment variables for storage commands
.PARAMETER storageAccount
   The storage account to use when provisioning test credentials
#>
function Provision-StorageCredentials
{
   param([string] $storageAccount);
   $storage = Get-AzureStorageKey $storageAccount;
   $connectionString = [string]::Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", $storageAccount, $storage.Primary);
   $global:StorageConnectionString = Get-Item env:AZURE_STORAGE_CONNECTION_STRING;
   Set-Item env:AZURE_STORAGE_CONNECTION_STRING $connectionString;
}

<#
.SYNOPSIS
Sets up environment variables for storage commands
#>
function Cleanup-StorageCredentials
{
    Set-Item env:AZURE_STORAGE_CONNECTION_STRING $global:StorageConnectionString;
}

<#
.SYNOPSIS
Tests using Get-AzureStorageContainer without container name.
#>
function Test-GetAzureStorageContainerWithoutContainerName
{
    $containers = Get-AzureStorageContainer; 
    Assert-True {$containers.Count -ge 1};
    $container =  $containers | ? {$_.Name -eq $containerName}
    Assert-NotNull $container;
}

<#
.SYNOPSIS
Tests using Get-AzureStorageContainer with container name.
#>
function Test-GetAzureStorageContainerWithContainerName
{
    $containers = Get-AzureStorageContainer $containerName; 
    Assert-True {$containers.Count -eq 1};
    Assert-AreEqual $containers[0].Name $containerName;
}

<#
.SYNOPSIS
Tests using Get-AzureStorageContainer with container prefix.
#>
function Test-GetAzureStorageContainerWithPrefix
{
    $containers = Get-AzureStorageContainer -Prefix $containerPrefix; 
    Assert-True {$containers.Count -ge 1};
    $containers | % {Assert-True {$_.Name.StartsWith($containerPrefix)}}
}

<#
.SYNOPSIS
Tests using New-AzureStorageContainer.
#>
function Test-NewAzureStorageContainer
{
    $randomName = [System.Guid]::NewGuid().ToString();
    $container = New-AzureStorageContainer $randomName;
    Assert-True {$container.Count -eq 1};
    Assert-True {$container[0].Name -eq $randomName}
    Assert-True {$container[0].PublicAccess.ToString() -eq "Off"}
	
    try
    {
        $container[0].CloudBlobContainer.DeleteIfExists();
    }
    catch
    {}
}

<#
.SYNOPSIS
Tests using New-AzureStorageContainer with specified acl level
#>
function Test-NewAzureStorageContainerWithPermission
{
    $randomName = [System.Guid]::NewGuid().ToString();
    $container = New-AzureStorageContainer $randomName -Permission Container;
    Assert-True {$container[0].Name -eq $randomName}
    Assert-True {$container[0].PublicAccess.ToString() -eq "Container"}
	
    try
    {
        $container[0].CloudBlobContainer.DeleteIfExists();
    }
    catch
    {}
}

<#
.SYNOPSIS
Tests using New-AzureStorageContainer to create a container which already exists
#>
function Test-NewExistsAzureStorageContainer
{
    Assert-Throws {New-AzureStorageContainer $containerName} "Container '$containerName' already exists."
}

<#
.SYNOPSIS
Tests using New-AzureStorageContainer with invalid container name
#>
function Test-NewExistsAzureStorageContainerWithInvalidContainerName
{
    $invalidName = "a";
    Assert-Throws {New-AzureStorageContainer $invalidName}
}

<#
.SYNOPSIS
Tests using Remove-AzureStorageContainer
#>
function Test-RemoveAzureStorageContainer
{
    $randomName = [System.Guid]::NewGuid().ToString();
    New-AzureStorageContainer $randomName
    Remove-AzureStorageContainer $randomName -Force
}

<#
.SYNOPSIS
Tests using Remove-AzureStorageContainer by container pipeline
#>
function Test-RemoveAzureStorageContainerByContainerPipeline
{
    $randomName = [System.Guid]::NewGuid().ToString();
    New-AzureStorageContainer $randomName | Get-AzureStorageContainer | Remove-AzureStorageContainer -Force
}