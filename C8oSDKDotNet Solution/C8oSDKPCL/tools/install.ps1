param($installPath, $toolsPath, $package, $project)
$DTE.ItemOperations.Navigate("http://www.convertigo.com?" + $package.Id + "=" + $from + ".." + $package.Version)
