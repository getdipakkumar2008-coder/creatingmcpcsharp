$ErrorActionPreference = 'Stop'
$cs = $env:EMPLOYEE_DB_CONNECTION
$fail = 0

function Check($label, $cond, $detail) {
    if ($cond) { Write-Host "PASS: $label ($detail)" }
    else { Write-Host "FAIL: $label ($detail)"; $script:fail++ }
}

# Read: get all
$all = Invoke-Sqlcmd -ConnectionString $cs -Query "EXEC dbo.usp_GetAllEmployees"
Check "GetAll returns rows" ($all.Count -ge 10) "count=$($all.Count)"

# Read: search by Id
$byId = Invoke-Sqlcmd -ConnectionString $cs -Query "EXEC dbo.usp_SearchEmployee @Id = 1"
Check "Search by Id=1 -> Dipak" ($byId.Name -eq 'Dipak') "name=$($byId.Name)"

# Read: search by Name
$byName = Invoke-Sqlcmd -ConnectionString $cs -Query "EXEC dbo.usp_SearchEmployee @Name = N'Rohan'"
Check "Search by Name=Rohan -> Id=3" ([int]$byName.Id -eq 3) "id=$($byName.Id)"

# Write paths on a disposable row (Id 9999) so real data is untouched.
Invoke-Sqlcmd -ConnectionString $cs -Query "DELETE FROM dbo.Employeedata WHERE Id = 9999;" | Out-Null
Invoke-Sqlcmd -ConnectionString $cs -Query "INSERT INTO dbo.Employeedata (Id, Name, Department, Email, Address, JoiningDate) VALUES (9999, N'TempUser', N'QA', N'temp@company.com', N'Nowhere', '2020-01-01');" | Out-Null

# Update: partial update should change only Department, keep Name
$upd = Invoke-Sqlcmd -ConnectionString $cs -Query "EXEC dbo.usp_UpdateEmployee @Id = 9999, @Department = N'Engineering'"
Check "Update returns updated row" ($upd.Id -eq 9999) "id=$($upd.Id)"
Check "Update changed Department" ($upd.Department -eq 'Engineering') "dept=$($upd.Department)"
Check "Update preserved Name (COALESCE)" ($upd.Name -eq 'TempUser') "name=$($upd.Name)"

# Update non-existent id returns no row
$updMiss = Invoke-Sqlcmd -ConnectionString $cs -Query "EXEC dbo.usp_UpdateEmployee @Id = 123456, @Name = N'X'"
Check "Update missing id returns nothing" ($null -eq $updMiss) "rows=$(@($updMiss).Count)"

# Delete: returns RowsAffected = 1
$del = Invoke-Sqlcmd -ConnectionString $cs -Query "EXEC dbo.usp_DeleteEmployee @Id = 9999"
Check "Delete reports 1 row" ([int]$del.RowsAffected -eq 1) "rows=$($del.RowsAffected)"

# Delete again: RowsAffected = 0
$del0 = Invoke-Sqlcmd -ConnectionString $cs -Query "EXEC dbo.usp_DeleteEmployee @Id = 9999"
Check "Delete again reports 0 rows" ([int]$del0.RowsAffected -eq 0) "rows=$($del0.RowsAffected)"

# Confirm row is gone
$gone = Invoke-Sqlcmd -ConnectionString $cs -Query "SELECT COUNT(*) AS C FROM dbo.Employeedata WHERE Id = 9999"
Check "Temp row removed" ([int]$gone.C -eq 0) "count=$($gone.C)"

if ($fail -eq 0) { Write-Host "`nALL STORED PROCEDURE VALIDATIONS PASSED" }
else { Write-Host "`n$fail VALIDATION(S) FAILED" }
