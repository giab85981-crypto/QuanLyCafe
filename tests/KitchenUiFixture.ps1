param([switch]$Cleanup)
$ErrorActionPreference='Stop'
$base='http://localhost:7063/api'
$fixturePath=Join-Path $PSScriptRoot 'kitchen-ui-fixture.json'
$login=Invoke-RestMethod -Uri "$base/Auth/login" -Method Post -ContentType 'application/json' -Body (@{userName='admin';passWord='admin123'}|ConvertTo-Json)
$headers=@{Authorization="Bearer $($login.token)"}
function Api($method,$path,$body=$null){$p=@{Uri="$base$path";Method=$method;Headers=$headers};if($null-ne$body){$p.ContentType='application/json';$p.Body=$body|ConvertTo-Json -Depth 8};Invoke-RestMethod @p}
if($Cleanup){
 foreach($id in (Get-Content $fixturePath|ConvertFrom-Json)){
  $bill=Api GET "/Orders/$id"
  if($bill.status-ne0 -or $bill.note-ne'Phiếu thử giao diện · ít đường, không đá.'){throw 'Fixture changed; do not modify it automatically.'}
  foreach($line in $bill.items){Api POST "/Bill/items/$($line.idBillInfo)/cancel" @{count=$line.count;reason='Dọn phiếu kiểm tra giao diện, món không có nguyên liệu.'}|Out-Null}
  Api POST "/Bill/$id/close-empty" | Out-Null
 }
 Remove-Item -LiteralPath $fixturePath
 Write-Output 'UI fixture closed; no payment or ingredient movement.'
 exit
}
$foods=Api GET '/Food'
$food=$foods|Where-Object {$_.isActive -and !$_.isTopping -and $_.recipe.Count-eq0 -and $_.variants.Count-eq0}|Select-Object -First 1
if(!$food){throw 'No recipe-free demo food available; fixture not created.'}
$ids=@()
foreach($stage in @('Chờ','Đang làm','Hoàn thành')){
 $id=(Api POST '/Orders/takeaway' @{requestKey=[guid]::NewGuid().ToString();label="Demo bếp · $stage";note='Phiếu thử giao diện · ít đường, không đá.'}).idBill
 $ids+= $id; $ids|ConvertTo-Json|Set-Content $fixturePath -Encoding utf8
 Api POST '/Bill/add-item' @{idBill=$id;idFood=$food.id;count=2}|Out-Null
 $ticket=(Api POST '/Kitchen/send-order' @{idBill=$id}).idKitchenOrder
 if($stage-ne'Chờ'){Api PUT "/Kitchen/$ticket/status" @{status='Cooking'}|Out-Null}
 if($stage-eq'Hoàn thành'){Api PUT "/Kitchen/$ticket/status" @{status='Completed'}|Out-Null}
}
Write-Output 'Three labeled recipe-free UI tickets ready; no payment or ingredient movement.'

