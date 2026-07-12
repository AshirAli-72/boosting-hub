# Requirements Document

## Introduction

This feature improves the three report hub components in the Boosting Hub admin dashboard
(`RevenueReportHub`, `TasksReportHub`, `UsersReportHub`). The current components are hardcoded
to show the last 7 days, silently swallow errors, have no export, no refresh, no trend context,
and use basic bar charts with no interactivity. The goal is to make all three reports more
actionable, robust, and professional without introducing heavy third-party chart libraries.

The improvements span both the service layer (date-range parameters) and the Blazor UI layer
(date picker, trend badges, CSV export, error states, manual refresh, chart tooltips, adaptive
labels, and summary donut/pie charts).

---

## Glossary

- **Report_Hub**: One of the three Blazor components — `RevenueReportHub`, `TasksReportHub`, or `UsersReportHub`.
- **Date_Range_Picker**: The shared UI control that lets the admin select a predefined or custom date range.
- **Active_Range**: The date range currently selected in the Date_Range_Picker.
- **Comparison_Period**: The period immediately before the Active_Range with the same duration (e.g., the 7 days before the current 7-day window).
- **Trend_Indicator**: The visual badge that shows the percentage change of a metric between the Active_Range and the Comparison_Period.
- **Bar_Chart**: The horizontal bar chart rendered with CSS/HTML divs inside each Report_Hub.
- **Donut_Chart**: A CSS-only circular chart showing proportional breakdown of statuses.
- **IReportService**: The C# service interface that supplies data to the Report_Hubs.
- **ReportFilter**: A parameter object carrying `DateFrom` and `DateTo` values passed to `IReportService` methods.
- **CSV_Export**: A client-side file download of the currently displayed report data in comma-separated-values format.
- **Error_State**: The UI region displayed inside a Report_Hub when data loading fails.
- **Preset**: One of the fixed date range shortcuts — Last 7 Days, Last 30 Days, Last 90 Days.

---

## Requirements

### Requirement 1: Date Range Filtering

**User Story:** As an admin, I want to filter all three report cards by a selected date range, so that I can analyze platform performance over any time window — not just the last 7 days.

#### Acceptance Criteria

1. THE `Date_Range_Picker` SHALL provide four selection options: **Last 7 Days**, **Last 30 Days**, **Last 90 Days**, and **Custom**.
2. WHEN the admin selects a **Preset**, THE `Date_Range_Picker` SHALL set `DateFrom` to the start of that preset period and `DateTo` to the current date (UTC).
3. WHEN the admin selects **Custom**, THE `Date_Range_Picker` SHALL display a start-date input and an end-date input that accept calendar dates.
4. WHEN the admin confirms a **Custom** range where `DateFrom` is after `DateTo`, THE `Date_Range_Picker` SHALL display a validation message "Start date must be before end date" and SHALL NOT trigger a data reload.
5. WHEN a valid `Active_Range` is selected, THE `Date_Range_Picker` SHALL broadcast the new range to all three `Report_Hub` components simultaneously.
6. THE `IReportService` SHALL expose updated method signatures: `GetRevenueReportAsync(ReportFilter filter)`, `GetUsersReportAsync(ReportFilter filter)`, and `GetTasksReportAsync(ReportFilter filter)`.
7. WHEN `IReportService` receives a `ReportFilter`, THE `IReportService` SHALL scope all aggregate counts and time-series data to records whose relevant date field falls within `[filter.DateFrom, filter.DateTo]` inclusive.
8. THE `Date_Range_Picker` SHALL default to **Last 7 Days** on initial page load.
9. WHILE a data reload is in progress after a range change, THE `Report_Hub` SHALL display a loading spinner in place of its content.

---

### Requirement 2: Trend Indicators

**User Story:** As an admin, I want each key metric to show its percentage change versus the previous equivalent period, so that I can quickly assess whether numbers are improving or declining.

#### Acceptance Criteria

1. THE `Report_Hub` SHALL compute a `Comparison_Period` whose duration equals that of the `Active_Range` and which ends the day before `Active_Range.DateFrom`.
2. WHEN the metric value for the `Active_Range` is greater than the value for the `Comparison_Period`, THE `Trend_Indicator` SHALL display a green upward-arrow icon and the percentage increase formatted as "+X.X%".
3. WHEN the metric value for the `Active_Range` is less than the value for the `Comparison_Period`, THE `Trend_Indicator` SHALL display a red downward-arrow icon and the percentage decrease formatted as "−X.X%".
4. WHEN the metric value for the `Active_Range` equals the value for the `Comparison_Period`, THE `Trend_Indicator` SHALL display a gray dash icon and the text "0.0%".
5. WHEN the `Comparison_Period` value is zero and the `Active_Range` value is greater than zero, THE `Trend_Indicator` SHALL display a green upward-arrow icon and the text "New".
6. THE `IReportService` SHALL return comparison-period values for each key metric alongside the active-period values in the same DTO, using a naming convention such as `PreviousTotalRevenue`, `PreviousTotalOrders`, etc.
7. THE `Trend_Indicator` SHALL be rendered adjacent to its corresponding metric value on each `Report_Hub` card.
8. THE `RevenueReportHub` SHALL display `Trend_Indicator` badges for: TotalRevenue, TotalOrders, ApprovedOrders, and AvgOrderValue.
9. THE `TasksReportHub` SHALL display `Trend_Indicator` badges for: TotalTasks, CompletedTasks, ApprovedProofs, and PendingProofs.
10. THE `UsersReportHub` SHALL display `Trend_Indicator` badges for: TotalUsers, ActiveUsers, VerifiedUsers, and JoinedToday.

---

### Requirement 3: CSV Export

**User Story:** As an admin, I want to download the currently displayed report data as a CSV file, so that I can share or further analyze it in a spreadsheet tool.

#### Acceptance Criteria

1. THE `Report_Hub` SHALL display an "Export CSV" button in its card header area.
2. WHEN the admin clicks "Export CSV", THE `Report_Hub` SHALL generate a CSV file containing the metric summary rows and the time-series rows for the currently visible `Active_Range`.
3. WHEN the admin clicks "Export CSV", THE `Report_Hub` SHALL trigger a browser file download with a filename in the format `{report-type}-{DateFrom:yyyy-MM-dd}-to-{DateTo:yyyy-MM-dd}.csv` (e.g., `revenue-2025-07-01-to-2025-07-07.csv`).
4. THE CSV file for `RevenueReportHub` SHALL include columns: Date, TotalRevenue, TotalOrders, ApprovedOrders, PendingOrders, RejectedOrders, AvgOrderValue.
5. THE CSV file for `TasksReportHub` SHALL include columns: Date, TotalTasks, ActiveTasks, CompletedTasks, PendingProofs, ApprovedProofs, RejectedProofs.
6. THE CSV file for `UsersReportHub` SHALL include columns: Date, TotalUsers, VerifiedUsers, UnverifiedUsers, ActiveUsers, LockedUsers, JoinedToday.
7. THE `Report_Hub` SHALL generate the CSV entirely client-side using Blazor JavaScript interop, without requiring a dedicated server endpoint.
8. WHILE a data reload is in progress, THE "Export CSV" button SHALL be disabled.

---

### Requirement 4: Error Handling

**User Story:** As an admin, I want to see a clear error message with a retry option when a report fails to load, so that I am aware of the failure and can take action rather than seeing stale or missing data silently.

#### Acceptance Criteria

1. WHEN `IReportService` throws an exception during data loading, THE `Report_Hub` SHALL catch the exception and transition to an `Error_State`.
2. WHEN in `Error_State`, THE `Report_Hub` SHALL display the message "Failed to load report data. Please try again." in a visually distinct error banner within the card.
3. WHEN in `Error_State`, THE `Report_Hub` SHALL display a "Retry" button inside the error banner.
4. WHEN the admin clicks "Retry", THE `Report_Hub` SHALL attempt to reload data and, if successful, SHALL dismiss the `Error_State` and display the loaded data.
5. WHEN the admin clicks "Retry" and the reload fails again, THE `Report_Hub` SHALL remain in `Error_State` and SHALL display the error banner again.
6. THE `Report_Hub` SHALL NOT display a loading spinner and an `Error_State` simultaneously.
7. THE `Report_Hub` SHALL NOT swallow exceptions silently with an empty `catch` block.

---

### Requirement 5: Manual Refresh

**User Story:** As an admin, I want a refresh button on each report card, so that I can reload the latest data without navigating away from the page.

#### Acceptance Criteria

1. THE `Report_Hub` SHALL display a refresh icon button in its card header area.
2. WHEN the admin clicks the refresh button, THE `Report_Hub` SHALL reload data for the currently active `Active_Range` from `IReportService`.
3. WHILE a refresh is in progress, THE `Report_Hub` SHALL display a loading spinner inside the card and SHALL disable the refresh button to prevent concurrent requests.
4. WHEN a refresh completes successfully, THE `Report_Hub` SHALL update all displayed metrics and charts with the new data.
5. WHEN a refresh fails, THE `Report_Hub` SHALL transition to `Error_State` as described in Requirement 4.
6. THE refresh button SHALL remain visible and operable at all times except during an in-progress reload.

---

### Requirement 6: Bar Chart Improvements

**User Story:** As an admin, I want the bar charts to show exact values on hover and use readable labels that adapt to the selected date range, so that I can read data points precisely without having to eyeball bar heights.

#### Acceptance Criteria

1. WHEN the admin hovers the cursor over a bar in the `Bar_Chart`, THE `Bar_Chart` SHALL display a tooltip containing the exact numeric value for that bar.
2. THE tooltip SHALL be implemented using CSS (`title` attribute or a CSS-driven tooltip pattern) without requiring a JavaScript chart library.
3. WHEN the `Active_Range` is **Last 7 Days**, THE `Bar_Chart` x-axis labels SHALL display day-level labels in the format "MMM dd" (e.g., "Jul 05").
4. WHEN the `Active_Range` is **Last 30 Days**, THE `Bar_Chart` x-axis labels SHALL display week-level labels in the format "Week of MMM dd".
5. WHEN the `Active_Range` is **Last 90 Days**, THE `Bar_Chart` x-axis labels SHALL display month-level labels in the format "MMM yyyy" (e.g., "Jul 2025").
6. WHEN the `Active_Range` is a **Custom** range of 1–14 days, THE `Bar_Chart` x-axis labels SHALL use day-level labels.
7. WHEN the `Active_Range` is a **Custom** range of 15–60 days, THE `Bar_Chart` x-axis labels SHALL use week-level labels.
8. WHEN the `Active_Range` is a **Custom** range of more than 60 days, THE `Bar_Chart` x-axis labels SHALL use month-level labels.
9. THE `IReportService` SHALL return time-series data aggregated at the appropriate granularity (daily, weekly, or monthly) based on the `ReportFilter` date span.

---

### Requirement 7: Summary Donut/Pie Charts

**User Story:** As an admin, I want a visual proportional breakdown of key statuses on each report card, so that I can instantly perceive the distribution without having to mentally calculate ratios from raw numbers.

#### Acceptance Criteria

1. THE `RevenueReportHub` SHALL display a `Donut_Chart` showing the proportional split of ApprovedOrders, PendingOrders, and RejectedOrders relative to TotalOrders.
2. THE `TasksReportHub` SHALL display a `Donut_Chart` showing the proportional split of ApprovedProofs, PendingProofs, and RejectedProofs relative to the total proof count.
3. THE `UsersReportHub` SHALL display a `Donut_Chart` showing the proportional split of ActiveUsers and LockedUsers relative to TotalUsers.
4. THE `Donut_Chart` SHALL be implemented using CSS only (conic-gradient or equivalent), with no external chart library dependency.
5. WHEN all segments of a `Donut_Chart` are zero, THE `Donut_Chart` SHALL display a single neutral-colored full circle and the label "No data".
6. THE `Donut_Chart` SHALL include a color-coded legend listing each segment name and its count alongside the chart.
7. WHEN the admin hovers over a segment of the `Donut_Chart`, THE `Donut_Chart` SHALL display a tooltip showing the segment name, count, and percentage of the total.
8. THE `Donut_Chart` tooltip SHALL be implemented using CSS without requiring JavaScript event handlers.
9. WHEN a `Report_Hub` is in `Error_State` or loading state, THE `Donut_Chart` SHALL NOT be rendered.
