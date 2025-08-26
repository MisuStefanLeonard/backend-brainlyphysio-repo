#!/bin/bash

set -ex  # Exit script on error

echo "🚀 Running database migrations..."

dotnet tool list --global || { echo "❌ dotnet-ef is missing!"; exit 1; }

export PATH="$PATH:/root/.dotnet/tools"  
dotnet ef database update --no-build --configuration Release

echo "✅ Migrations applied successfully!"

echo "🚀 Running Quartz SQL script..."

required_tables=(QRTZ_JOB_DETAILS QRTZ_TRIGGERS QRTZ_SIMPLE_TRIGGERS QRTZ_CRON_TRIGGERS QRTZ_SIMPROP_TRIGGERS QRTZ_BLOB_TRIGGERS QRTZ_CALENDARS QRTZ_PAUSED_TRIGGER_GRPS QRTZ_FIRED_TRIGGERS QRTZ_SCHEDULER_STATE QRTZ_LOCKS)

# Create a comma-separated list for the query
table_list=$(printf "'%s'," "${required_tables[@]}")
table_list=${table_list%,}  # Remove trailing comma
echo "🚀🚀 Am ajuns aici 🚀🚀"
echo "Table list  ${table_list}"
echo "🚀✅🚀 Am ajuns aici 2222222 🚀✅🚀"

# Run the query to count the number of tables present
existing_count=$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -s -N -e "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='$DB_NAME' AND TABLE_NAME IN ($table_list);")

expected_count=${#required_tables[@]}
echo "Existing count ${existing_count}"
echo "Expected count ${expected_count}"
echo "✅🚀✅ Am ajuns aici 3 ✅🚀✅"

if [ "$existing_count" -eq "$expected_count" ]; then
    echo "All required tables exist. Skipping table/index creation."
else
    echo "Some required tables are missing. Running creation script..."
    mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" < ./quartz_init.sql
fi

exit 0;

