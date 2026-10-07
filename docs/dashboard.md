# Dashboard (through menu performance)

Run the API with `dotnet run --project CafeManagement.API --launch-profile https` and frontend with `npm.cmd run dev` from `frontend`.

## Database

Migration `20261003090000_DashboardSalesTracking` adds nullable Bill.GuestCount and BillInfo.UnitPrice, Food.MenuKind and ItemType catalog. Existing rows are preserved. The local QuanLyCafe_V2 database was upgraded during this task.

For another database that has migrations through `20261002031845_AddItemTypeToFood`, apply the idempotent SQL script `CafeManagement.API/Migrations/dashboard-upgrade.sql`, or use EF migrations. Do not use EnsureCreated to upgrade an existing database; it does not apply migrations.

## Calculations

- Paid bills only (Status=1), grouped by DateCheckOut with DateCheckIn fallback for old bills.
- Periods include today, using Vietnam local dates. Existing bill timestamps are assumed to be Vietnam local wall-clock time.
- Net revenue uses stored TotalPrice. Legacy zero totals without price snapshots are estimated from current food prices after percentage discount and flagged in the UI. No historical totals are overwritten.
- Menu net revenue allocates the bill net total proportionally to line gross amounts. Weighted averages divide net revenue by sold item quantities, including free items. Type/category filters apply before selecting Top 10; averages and distribution cover the full period.
- Guest count is entered in POS, saved on blur for serving bills and at checkout. New bills default to one guest. Legacy unknown counts remain null and are excluded from guest totals.
- MenuKind (Đồ ăn / Đồ uống / Khác) is separate from existing ItemType (Món chế biến / Hàng hóa / Dịch vụ). Update MenuKind in the food edit form to enable the food/drink averages. Existing classifications are not guessed.
- Only active tables contribute to coverage. This database currently has no active tables; activate the appropriate tables through table management for a nonzero denominator.
- Return/refund and VAT data are not yet modeled, so refunds are displayed as unsupported and revenue does not claim VAT treatment.
- Current model supports one branch. The header shows the central branch without a nonfunctional multi-branch filter.

## Verification

`dotnet run --project tests/DashboardChecks -- CafeManagement.API/Migrations/dashboard-upgrade.sql` checks aggregation, checkout-date boundaries, zero and legacy data, free bills, price changes, filters, validation and migration/model consistency.

Add `--database` to verify checkout and dashboard queries against configured LocalDB. Integration fixtures run inside a transaction that is always rolled back. This consumes identity values but retains no test rows.

Frontend validation: `npm.cmd run build` and `npm.cmd run lint`. Existing unrelated lint/nullability and bundle-size warnings remain.
## LocalDB startup troubleshooting

In Windows Development, `LocalDb:UseNamedPipe=true` makes the API query the installed SqlLocalDB utility for the current instance pipe before registering DbContext. If the instance is stopped, the utility starts it first. The original database name and Windows authentication settings remain unchanged. Production and non-LocalDB connection strings do not use this helper.

The direct pipe is resolved at every API startup, not persisted in appsettings, because its name changes when LocalDB restarts. If LocalDB is restarted while the API is running, restart the API too. Microsoft documents this connection method at https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb .

The affected F5 process returned LocalDB startup error 50 / Windows error 575 while the same database was accessible from a separate process. The helper avoids that instance-discovery path; the underlying Windows/runtime cause is not yet established. Login and Dashboard were verified on port 7053 after this change.

Run `dotnet run --project tests/DashboardChecks -- CafeManagement.API/Migrations/dashboard-upgrade.sql --localdb` for pipe parsing and direct connection checks.