/*******************************************************************************
* Copyright 2009-2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace AwsS3Select
{
    class S3Sample
    {
        // Change the AWSProfileName to the profile you want to use in the App.config file.
        // See http://aws.amazon.com/credentials  for more details.
        // You must also sign up for an Amazon S3 account for this to work
        // See http://aws.amazon.com/s3/ for details on creating an Amazon S3 account
        // Change the bucketName and keyName fields to values that match your bucketname and keyname

        // I just play Amazon S3 Select in the AWS SDK for .NET
        // See https://aws.amazon.com/blogs/developer/amazon-s3-select-support-in-the-aws-sdk-for-net/

        static string bucketName = "javasorn-s3select";
        static string keyName = "S3Object.json";
        static IAmazonS3 client;

        public static void Main(string[] args)
        {
            client = new AmazonS3Client();
            Console.WriteLine("Wait...");
            var someTask = DoSelectFromS3();
            someTask.Wait();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        private static async Task DoSelectFromS3()
        {
            var endWaitHandle = new AutoResetEvent(false);

            using (var eventStream = await GetSelectObjectContentEventStream())
            {
                // Since everything happens on a background thread, exceptions are raised as events.
                // Here, we are just throwing the exception received.
                eventStream.ExceptionReceived += (sender, args) => throw args.EventStreamException;

                eventStream.EventReceived += (sender, args) =>
                    Console.WriteLine($"Received {args.EventStreamEvent.GetType().Name}!");
                eventStream.RecordsEventReceived += (sender, args) =>
                {
                    Console.WriteLine("The contents of the Records Event is...");
                    using (var reader = new StreamReader(args.EventStreamEvent.Payload, Encoding.UTF8))
                    {
                        Console.Write(reader.ReadToEnd());
                    }
                };
                eventStream.EndEventReceived += (sender, args) => endWaitHandle.Set();

                eventStream.StartProcessing();
                endWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
            }
        }

        private static async Task<ISelectObjectContentEventStream> GetSelectObjectContentEventStream()
        {
            var response = await client.SelectObjectContentAsync(new SelectObjectContentRequest()
            {
                Bucket = bucketName,
                Key = keyName,
                ExpressionType = ExpressionType.SQL,
                Expression = "select * from S3Object s where s.COMPANY = 'AMAZON'",
                InputSerialization = new InputSerialization()
                {
                    JSON = new JSONInput()
                    {
                        JsonType = JsonType.Lines
                    }
                },
                OutputSerialization = new OutputSerialization()
                {
                    JSON = new JSONOutput()
                }
            });

            return response.Payload;
        }
    }
}