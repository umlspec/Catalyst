# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: MempoolBuf.proto

require 'google/protobuf'

Google::Protobuf::DescriptorPool.generated_pool.build do
  add_message "Catalyst.Protocols.Mempool.Tx" do
    optional :addressSource, :string, 1
    optional :addressDest, :string, 2
    optional :signature, :string, 3
    optional :amount, :fixed32, 4
    optional :fee, :fixed32, 5
    optional :outputAmount, :fixed32, 6
    optional :inputAction, :string, 7
    optional :unlockScript, :string, 8
    optional :unlockingProgram, :string, 9
    optional :updated, :message, 10, "Catalyst.Protocols.Mempool.Tx.Timestamp"
  end
  add_message "Catalyst.Protocols.Mempool.Tx.Timestamp" do
    optional :seconds, :int64, 1
    optional :nanos, :int32, 2
  end
  add_message "Catalyst.Protocols.Mempool.Key" do
    optional :hashed_signature, :string, 1
  end
end

module Catalyst
  module Protocols
    module Mempool
      Tx = Google::Protobuf::DescriptorPool.generated_pool.lookup("Catalyst.Protocols.Mempool.Tx").msgclass
      Tx::Timestamp = Google::Protobuf::DescriptorPool.generated_pool.lookup("Catalyst.Protocols.Mempool.Tx.Timestamp").msgclass
      Key = Google::Protobuf::DescriptorPool.generated_pool.lookup("Catalyst.Protocols.Mempool.Key").msgclass
    end
  end
end