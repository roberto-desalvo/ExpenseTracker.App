const config = {
    expenseTrackerBaseUrl: import.meta.env.VITE_EXPENSE_TRACKER_API_BASE_URL,
    expenseTrackerApiVersion: import.meta.env.VITE_EXPENSE_TRACKER_API_VERSION,
    expenseTrackerAccountUrl: import.meta.env.VITE_EXPENSE_TRACKER_API_ACCOUNT_URL,
    expenseTrackerTransactionUrl: import.meta.env.VITE_EXPENSE_TRACKER_API_TRANSACTION_URL,
    expenseTrackerCategoryUrl: import.meta.env.VITE_EXPENSE_TRACKER_API_CATEGORY_URL,
    expenseTrackerTransferUrl: import.meta.env.VITE_EXPENSE_TRACKER_API_TRANSFER_URL,
}

export default config;