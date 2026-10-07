$base='http://localhost:7063/api'
$checks=0
function CallApi($method,$path,$body=$null,$token=$null) {
 $headers=@{}; if($token){$headers.Authorization="Bearer $token"}
 $params=@{ Uri="$base$path"; Method=$method; Headers=$headers; SkipHttpErrorCheck=$true }
 if($null -ne $body){$params.Body=($body|ConvertTo-Json -Depth 8);$params.ContentType='application/json; charset=utf-8'}
 $r=Invoke-WebRequest @params
 [pscustomobject]@{Status=[int]$r.StatusCode;Data=if($r.Content){try{$r.Content|ConvertFrom-Json}catch{$r.Content}}else{$null}}
}
function Assert($condition,$name) {if(!$condition){throw "FAIL $name"};$script:checks++;Write-Output "PASS $name"}
Assert ((CallApi GET '/Employee').Status -eq 401) 'Anonymous employee list blocked'
Assert ((CallApi GET '/Food').Status -eq 401) 'Anonymous menu management blocked'
$admin=CallApi POST '/Auth/login' @{userName='admin';passWord='admin123'}
Assert ($admin.Status -eq 200) 'Admin login'
$at=$admin.Data.token
$roles=(CallApi GET '/Account/roles' $null $at).Data
$employees=(CallApi GET '/Employee' $null $at).Data.items
$cash=$employees|Where-Object userName -eq 'staff.cashier'
if(!$cash){
 $cash=(CallApi POST '/Employee' @{name='Mai · thu ngân demo';phone='0906000011';department='Vận hành';position='Thu ngân';hireDate='2026-10-01';note='Nhân viên mẫu kiểm tra vai trò.'} $at).Data
 Assert ((CallApi POST "/Employee/$($cash.id)/login" @{userName='staff.cashier';passWord='DemoCafe123';idRole=($roles|Where-Object name -eq Cashier).id} $at).Status -eq 200) 'Create cashier login'
}
$kitchen=$employees|Where-Object userName -eq 'staff.kitchen'
if(!$kitchen){
 $kitchen=(CallApi POST '/Employee' @{name='Nam · pha chế demo';phone='0906000012';department='Bếp / Bar';position='Pha chế';hireDate='2026-10-01'} $at).Data
 Assert ((CallApi POST "/Employee/$($kitchen.id)/login" @{userName='staff.kitchen';passWord='DemoCafe123';idRole=($roles|Where-Object name -eq Kitchen).id} $at).Status -eq 200) 'Create kitchen login'
}
$ct=(CallApi POST '/Auth/login' @{userName='staff.cashier';passWord='DemoCafe123'}).Data.token
$kt=(CallApi POST '/Auth/login' @{userName='staff.kitchen';passWord='DemoCafe123'}).Data.token
Assert ((CallApi GET '/Employee' $null $ct).Status -eq 403) 'Cashier cannot read employee private profiles'
Assert ((CallApi GET '/Account' $null $ct).Status -eq 403) 'Cashier cannot read accounts'
Assert ((CallApi GET '/Dashboard/overview' $null $ct).Status -eq 403) 'Cashier cannot read management dashboard'
Assert ((CallApi GET '/Food' $null $ct).Status -eq 200) 'Cashier can load POS menu'
Assert ((CallApi POST '/Food' @{name='Denied'} $ct).Status -eq 403) 'Cashier cannot edit menu'
Assert ((CallApi GET '/Orders' $null $ct).Status -eq 403) 'Cashier cannot read management order history'
Assert ((CallApi GET '/Kitchen/pending-orders' $null $ct).Status -eq 403) 'Cashier cannot view kitchen queue'
Assert ((CallApi PUT '/Kitchen/1/status' @{status='Cooking'} $ct).Status -eq 403) 'Cashier cannot update preparation'
Assert ((CallApi GET '/Customer/manage' $null $ct).Status -eq 403) 'Cashier cannot manage customer list'
Assert ((CallApi GET '/Customer?search=Lan' $null $ct).Status -eq 200) 'Cashier can select POS customer'
Assert ((CallApi GET '/TableFood' $null $ct).Status -eq 200) 'Cashier can load POS tables'
Assert ((CallApi GET '/Cashbook' $null $ct).Status -eq 403) 'Cashier cannot read cashbook'
Assert ((CallApi POST '/Orders/11/refund' @{reason='Denied';paymentMethod='Cash'} $ct).Status -eq 403) 'Cashier cannot refund'
Assert ((CallApi POST '/Cashbook' @{amount=1} $ct).Status -eq 403) 'Cashier cannot create cash expense'
Assert ((CallApi GET '/Kitchen/pending-orders' $null $kt).Status -eq 200) 'Kitchen can view queue'
Assert ((CallApi GET '/Orders' $null $kt).Status -eq 403) 'Kitchen cannot read invoices'
Assert ((CallApi POST '/Kitchen/send-order' @{idBill=11} $kt).Status -eq 403) 'Kitchen cannot issue new stock-consuming ticket'
Assert ((CallApi PUT '/Account/admin/status' @{isActive=$false} $at).Status -eq 400) 'HTTP self lock protection'
Assert ((CallApi PUT '/Account/staff.cashier/status' @{isActive=$false} $at).Status -eq 200) 'Lock cashier'
Assert ((CallApi GET '/Food' $null $ct).Status -eq 401) 'Existing token revoked after lock'
Assert ((CallApi POST '/Auth/login' @{userName='staff.cashier';passWord='DemoCafe123'}).Status -eq 401) 'Locked login rejected'
Assert ((CallApi PUT '/Account/staff.cashier/status' @{isActive=$true} $at).Status -eq 200) 'Unlock cashier'
Assert ((CallApi GET '/Food' $null $ct).Status -eq 401) 'Unlock does not revive old token'
$ct=(CallApi POST '/Auth/login' @{userName='staff.cashier';passWord='DemoCafe123'}).Data.token
Assert ((CallApi GET '/Food' $null $ct).Status -eq 200) 'Fresh token works after unlock'
Assert ((CallApi PUT '/Account/staff.cashier' @{displayName='Mai · thu ngân demo';idRole=($roles|Where-Object name -eq Cashier).id;passWord='DemoCafe123'} $at).Status -eq 200) 'Demo password reset'
Assert ((CallApi GET '/Food' $null $ct).Status -eq 401) 'Reset revokes old HTTP token'
$ct=(CallApi POST '/Auth/login' @{userName='staff.cashier';passWord='DemoCafe123'}).Data.token
Assert ((CallApi PUT '/Account/staff.cashier' @{displayName='Mai · thu ngân demo';idRole=($roles|Where-Object name -eq Kitchen).id} $at).Status -eq 200) 'Change demo role'
Assert ((CallApi GET '/Orders' $null $ct).Status -eq 401) 'Role change revokes old HTTP token'
$changed=(CallApi POST '/Auth/login' @{userName='staff.cashier';passWord='DemoCafe123'}).Data.token
Assert ((CallApi GET '/Orders' $null $changed).Status -eq 403) 'New role enforced immediately'
Assert ((CallApi PUT '/Account/staff.cashier' @{displayName='Mai · thu ngân demo';idRole=($roles|Where-Object name -eq Cashier).id} $at).Status -eq 200) 'Restore cashier demo role'
Assert ((CallApi GET '/PublicMenu/1').Status -eq 200) 'Public QR menu remains available'
Write-Output "$checks HTTP checks passed"
