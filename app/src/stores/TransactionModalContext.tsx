import React, { createContext, useContext, useState, ReactNode } from "react";
import Transaction from "../models/Transaction";
import Account from "../models/Account";
import Category from "../models/Category";
import { useTransactions } from "./TransactionContext";

interface TransactionModalContextType {
  transactionModalOpen: boolean;
  currentTransaction: Transaction | null;
  openTransactionModal: () => void;
  closeTransactionModal: () => void;
  modifyCategory: (category: Category | null) => void;
  modifyDescription: (description: string) => void;
  modifyAmount: (amount: number) => void;
  modifyAccount: (account: Account | null) => void;
  modifyDate: (date: Date | undefined) => void;
  sendTransaction: () => void;
  setTransaction: (transaction: Transaction) => void;
}

const TransactionModalContext = createContext<
  TransactionModalContextType | undefined
>(undefined);

export const TransactionModalProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [transactionModalOpen, setTransactionModalOpen] =
    useState<boolean>(false);

  const [currentTransaction, setCurrentTransaction] =
    useState<Transaction>(new Transaction(0, new Date(), "", 0, 0, "", 0, ""));

  const transactionContext = useTransactions();

  const openTransactionModal = () => {
    setTransactionModalOpen(true);
  };

  const closeTransactionModal = () => {
    setTransactionModalOpen(false);
  };

  const setTransaction = (transaction: Transaction) => {
    setCurrentTransaction(transaction);
  }

  const modifyCategory = (category: Category | null) => {
    if (category === null) {
      return;
    }

    const transaction =
      currentTransaction ?? new Transaction(0, new Date(), "", 0, 0, "", 0, "");
    transaction.category = category.description;
    transaction.categoryId = category.id;
    setCurrentTransaction(transaction);
  };

  const modifyDescription = (description: string) => {
    const transaction =
      currentTransaction ?? new Transaction(0, new Date(), "", 0, 0, "", 0, "");
    transaction.description = description;
    setCurrentTransaction(transaction);
  };

  const modifyAmount = (amount: number) => {
    const transaction =
      currentTransaction ?? new Transaction(0, new Date(), "", 0, 0, "", 0, "");
    transaction.amount = amount;
    setCurrentTransaction(transaction);
  };

  const modifyAccount = (account: Account | null) => {
    if (account === null) {
      return;
    }
    const transaction =
      currentTransaction ?? new Transaction(0, new Date(), "", 0, 0, "", 0, "");
    transaction.account = account.name;
    transaction.accountId = account.id;
    setCurrentTransaction(transaction);
  };

  const modifyDate = (date: Date | undefined) => {
    if (date === null) {
      return;
    }
    const transaction =
      currentTransaction ?? new Transaction(0, new Date(), "", 0, 0, "", 0, "");
    transaction.date = date ?? new Date();
    setCurrentTransaction(transaction);
  };

  const sendTransaction = () => {

    console.log(currentTransaction);
    if (currentTransaction == null) {
      return;
    }

    if (currentTransaction.id > 0) {
      transactionContext.updateTransaction(
        currentTransaction.id,
        currentTransaction
      );
    } else {
      transactionContext.addTransaction(currentTransaction);
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
        setTransaction
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
