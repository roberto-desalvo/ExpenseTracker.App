import React, { createContext, useContext, useState, ReactNode } from "react";
import Transaction from "../models/Transaction";
import Account from "../models/Account";
import Category from "../models/Category";
import { useTransactions } from "./TransactionContext";

interface TransactionModalContextType {
  transactionModalOpen: boolean;
  currentTransaction: Transaction | null;
  openTransactionModal: (transaction?: Transaction | null) => void;
  closeTransactionModal: () => void;
  modifyCategory: (category: Category | null) => void;
  modifyDescription: (description: string) => void;
  modifyAmount: (amount: number) => void;
  modifyAccount: (account: Account | null) => void;
  modifyDate: (date: Date | undefined) => void;
  sendTransaction: () => Promise<void>;
}

const TransactionModalContext = createContext<
  TransactionModalContextType | undefined
>(undefined);

export const TransactionModalProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [transactionModalOpen, setTransactionModalOpen] =
    useState<boolean>(false);

  const [currentTransaction, setCurrentTransaction] = useState<Transaction | null>(null);

  const createEmptyTransaction = () =>
    new Transaction(0, new Date(), "", 0, 0, "", 0, "");

  const cloneTransaction = (transaction: Transaction) =>
    new Transaction(
      transaction.id,
      new Date(transaction.date),
      transaction.description,
      transaction.amount,
      transaction.categoryId,
      transaction.category,
      transaction.accountId,
      transaction.account
    );

  const openTransactionModal = (transaction?: Transaction | null) => {
    setCurrentTransaction(
      transaction ? cloneTransaction(transaction) : createEmptyTransaction()
    );
    setTransactionModalOpen(true);
  };

  const transactionContext = useTransactions();

  const closeTransactionModal = () => {
    setTransactionModalOpen(false);
  };

  const modifyCategory = (category: Category | null) => {
    if (category === null) {
      return;
    }

    setCurrentTransaction((prev) => {
      const transaction = prev ? cloneTransaction(prev) : createEmptyTransaction();
      transaction.category = category.description;
      transaction.categoryId = category.id;
      return transaction;
    });
  };

  const modifyDescription = (description: string) => {
    setCurrentTransaction((prev) => {
      const transaction = prev ? cloneTransaction(prev) : createEmptyTransaction();
      transaction.description = description;
      return transaction;
    });
  };

  const modifyAmount = (amount: number) => {
    setCurrentTransaction((prev) => {
      const transaction = prev ? cloneTransaction(prev) : createEmptyTransaction();
      transaction.amount = amount;
      return transaction;
    });
  };

  const modifyAccount = (account: Account | null) => {
    if (account === null) {
      return;
    }
    setCurrentTransaction((prev) => {
      const transaction = prev ? cloneTransaction(prev) : createEmptyTransaction();
      transaction.account = account.name;
      transaction.accountId = account.id;
      return transaction;
    });
  };

  const modifyDate = (date: Date | undefined) => {
    if (date === null) {
      return;
    }
    setCurrentTransaction((prev) => {
      const transaction = prev ? cloneTransaction(prev) : createEmptyTransaction();
      transaction.date = date ?? new Date();
      return transaction;
    });
  };

  const sendTransaction = async () => {
    if (currentTransaction == null) {
      return;
    }

    if (currentTransaction.id > 0) {
      await transactionContext.updateTransaction(currentTransaction);
    } else {
      await transactionContext.addTransaction(currentTransaction);
    }
  };

  return (
    <TransactionModalContext.Provider
      value={{
        transactionModalOpen,
        currentTransaction,
        openTransactionModal,
        closeTransactionModal,
        modifyAccount,
        modifyAmount,
        modifyCategory,
        modifyDate,
        modifyDescription,
        sendTransaction,
      }}
    >
      {children}
    </TransactionModalContext.Provider>
  );
};

export const useTransactionModal = (): TransactionModalContextType => {
  const context = useContext(TransactionModalContext);
  if (!context) {
    throw new Error(
      "useTransactions deve essere utilizzato all’interno di TransactionsProvider"
    );
  }
  return context;
};
