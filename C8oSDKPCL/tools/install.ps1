param($installPath, $toolsPath, $package, $project)
$DTE.ItemOperations.Navigate("http://www.convertigo.com/convertigo-sdk/?" + $package.Id + "=" + $from + ".." + $package.Version)
