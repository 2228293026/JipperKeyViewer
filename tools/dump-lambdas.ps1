param([string]$TypeName, [string[]]$NamePatterns)
Add-Type -Path 'D:\Projects\JipperKeyViewer\Libs\dnlib.dll'
$mod = [dnlib.DotNet.ModuleDefMD]::Load('D:\Projects\JipperKeyViewer\libs\Assembly-CSharp.dll')
$t = $mod.Types | Where-Object { $_.Name -eq $TypeName }
foreach ($nt in $t.NestedTypes) {
  foreach ($m in $nt.Methods) {
    foreach ($p in $NamePatterns) {
      if ($m.Name -like $p) {
        Write-Output ("===== " + $nt.Name + "::" + $m.FullName + " =====")
        if ($m.HasBody) {
          foreach ($i in $m.Body.Instructions) {
            Write-Output ("  " + $i.OpCode.Code.ToString() + " " + $i.Operand)
          }
        }
      }
    }
  }
}
