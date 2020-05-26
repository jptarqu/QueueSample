﻿//----------------------------------------------------------------------------------
// Microsoft Azure Storage Team
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Threading.Tasks;

namespace QueueStorage
{
    // Create queue, insert message, peek message, read message, change contents of queued message, 
    //    queue 20 messages, get queue length, read 20 messages, delete queue.

    public class GettingStarted
    {

        private const string approversInQueueName = "approvers-input";
        private const string approversOutqueueName = "approvers-output";
        private double duration(DateTime b) =>
            (DateTime.Now - b).TotalSeconds;
        /// <summary>
        /// Test some of the file storage operations.
        /// </summary>
        public async Task RunQueueStorageOperationsAsync()
        {
            // Create the queue name -- use a guid in the name so it's unique.
            

            var startTime = DateTime.Now;
            // Create or reference an existing queue.
            CloudQueue inputQueue = CreateQueueAsync(approversInQueueName).Result;
            CloudQueue outputQueue = CreateQueueAsync(approversOutqueueName).Result;
            Console.WriteLine("CreateQueueAsync took " + duration(startTime));

            startTime = DateTime.Now;
            // Demonstrate basic queue functionality.  
            await BasicQueueOperationsAsync(inputQueue, outputQueue);
            Console.WriteLine("BasicQueueOperationsAsync took " + duration(startTime));

            //startTime = DateTime.Now;
            //// Demonstrate how to update an enqueued message 
            //await UpdateEnqueuedMessageAsync(queue);
            //Console.WriteLine("UpdateEnqueuedMessageAsync took " + duration(startTime));

            //startTime = DateTime.Now;
            //// Demonstrate advanced functionality such as processing of batches of messages 
            //await ProcessBatchOfMessagesAsync(queue);
            //Console.WriteLine("ProcessBatchOfMessagesAsync took " + duration(startTime));

            // When you delete a queue it could take several seconds before you can recreate a queue with the same 
            // name - hence to enable you to run the demo in quick succession the queue is not deleted. If you want  
            // to delete the queue uncomment the line of code below.  
            // await DeleteQueueAsync(queue); 

        }

        /// <summary>
        /// Create a queue for the sample application to process messages in. 
        /// </summary>
        /// <returns>A CloudQueue object</returns>
        public async Task<CloudQueue> CreateQueueAsync(string queueName)
        {
            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a queue client for interacting with the queue service
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            Console.WriteLine("1. Create a queue for the demo");

            CloudQueue queue = queueClient.GetQueueReference(queueName);
            try
            {
                await queue.CreateIfNotExistsAsync();
            }
            catch 
            {
                Console.WriteLine("If you are running with the default configuration please make sure you have started the storage emulator.  ess the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return queue;
        }

        /// <summary>
        /// Demonstrate basic queue operations such as adding a message to a queue, peeking at the front of the queue and dequeing a message.
        /// </summary>
        /// <param name="inputQueue">The sample queue</param>
        public async Task BasicQueueOperationsAsync(CloudQueue inputQueue, CloudQueue outQueue)
        {
            // Insert a message into the queue using the AddMessage method. 

            // Peek at the message in the front of a queue without removing it from the queue using PeekMessage (PeekMessages lets you peek >1 message)
            Console.WriteLine("3. Peek at the next message");
            CloudQueueMessage peekedMessage = await inputQueue.PeekMessageAsync();
            if (peekedMessage != null)
            {
                Console.WriteLine("The peeked message is: {0}", peekedMessage.AsString);
            }
            Console.WriteLine("2. Insert a single message into a queue");
            await outQueue.AddMessageAsync(new CloudQueueMessage("Hello World!"));

            // You de-queue a message in two steps. Call GetMessage at which point the message becomes invisible to any other code reading messages 
            // from this queue for a default period of 30 seconds. To finish removing the message from the queue, you call DeleteMessage. 
            // This two-step process ensures that if your code fails to process a message due to hardware or software failure, another instance 
            // of your code can get the same message and try again. 
            //Console.WriteLine("4. De-queue the next message");
            //CloudQueueMessage message = await queue.GetMessageAsync();
            //if (message != null)
            //{
            //    Console.WriteLine("Processing & deleting message with content: {0}", message.AsString);
            //    await queue.DeleteMessageAsync(message);
            //}
        }

        /// <summary>
        /// Update an enqueued message and its visibility time. For workflow scenarios this could enable you to update 
        /// the status of a task as well as extend the visibility timeout in order to provide more time for a client 
        /// continue working on the message before another client can see the message. 
        /// </summary>
        /// <param name="queue">The sample queue</param>
        public async Task UpdateEnqueuedMessageAsync(CloudQueue queue)
        {
            // Insert another test message into the queue 
            Console.WriteLine("5. Insert another test message ");
            await queue.AddMessageAsync(new CloudQueueMessage("Hello World Again!"));

            Console.WriteLine("6. Change the contents of a queued message");
            CloudQueueMessage message = await queue.GetMessageAsync();
            message.SetMessageContent("Updated contents.");
            await queue.UpdateMessageAsync(
                message,
                TimeSpan.Zero,  // For the purpose of the sample make the update visible immediately
                MessageUpdateFields.Content |
                MessageUpdateFields.Visibility);
        }

        /// <summary>
        /// Demonstrate adding a number of messages, checking the message count and batch retrieval of messages. During retrieval we 
        /// also set the visibility timeout to 5 minutes. Visibility timeout is the amount of time message is not visible to other 
        /// clients after a GetMessageOperation assuming DeleteMessage is not called. 
        /// </summary>
        /// <param name="queue">The sample queue</param>
        public async Task ProcessBatchOfMessagesAsync(CloudQueue queue)
        {
            // Enqueue 20 messages by which to demonstrate batch retrieval
            Console.WriteLine("7. Enqueue 20 messages.");
            for (int i = 0; i < 20; i++)
            {
                await queue.AddMessageAsync(new CloudQueueMessage(string.Format("{0} - {1}", i, "Hello World")));
            }

            // The FetchAttributes method asks the Queue service to retrieve the queue attributes, including an approximation of message count 
            Console.WriteLine("8. Get the queue length");
            queue.FetchAttributes();
            int? cachedMessageCount = queue.ApproximateMessageCount;
            Console.WriteLine("Number of messages in queue: {0}", cachedMessageCount);

            // Dequeue a batch of 21 messages (up to 32) and set visibility timeout to 5 minutes. Note we are dequeuing 21 messages because the earlier
            // UpdateEnqueuedMessage method left a message on the queue hence we are retrieving that as well. 
            Console.WriteLine("9. Dequeue 21 messages, allowing 5 minutes for the clients to process.");
            foreach (CloudQueueMessage msg in await queue.GetMessagesAsync(21, TimeSpan.FromMinutes(5), null, null))
            {
                Console.WriteLine("Processing & deleting message with content: {0}", msg.AsString);

                // Process all messages in less than 5 minutes, deleting each message after processing.
                await queue.DeleteMessageAsync(msg);
            }
        }


        /// <summary>
        /// Delete the queue that was created for this sample
        /// </summary>
        /// <param name="queue">The sample queue to delete</param>
        public async Task DeleteQueueAsync(CloudQueue queue)
        {
            Console.WriteLine("10. Delete the queue");
            await queue.DeleteAsync();
        }

    }
}
