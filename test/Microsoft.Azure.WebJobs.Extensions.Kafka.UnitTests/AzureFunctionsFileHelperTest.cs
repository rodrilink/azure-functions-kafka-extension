﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class AzureFunctionsFileHelperTest : IDisposable
    {
        Dictionary<string, string> envVarsToRestore = new Dictionary<string, string>();

        private void SetEnvironmentVariable(string variable, string value)
        {
            if (!this.envVarsToRestore.ContainsKey(variable))
            {
                this.envVarsToRestore[variable] = Environment.GetEnvironmentVariable(variable);
            }

            Environment.SetEnvironmentVariable(variable, value, EnvironmentVariableTarget.Process);
        }

        void SetRunningInAzureEnvVars()
        {
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureFunctionWorkerRuntimeEnvVarName, "dotnet");            
        }

        public void Dispose()
        {
            foreach (var kv in envVarsToRestore)
            {
                Environment.SetEnvironmentVariable(kv.Key, kv.Value, EnvironmentVariableTarget.Process);
            }

            this.envVarsToRestore.Clear();
        }

        [Fact]
        public void IsFunctionRunningInAzure_When_Does_Not_Have_Azure_EnvVars_Should_Returns_False()
        {
            Assert.False(AzureFunctionsFileHelper.IsRunningAsFunctionInAzureOrContainer());
        }

        [Fact]
        public void IsFunctionRunningInAzure_When_Does_Have_Azure_EnvVars_Should_Returns_True()
        {
            SetRunningInAzureEnvVars();
            Assert.True(AzureFunctionsFileHelper.IsRunningAsFunctionInAzureOrContainer());
        }

        [Fact]
        public void GetAzureFunctionBaseFolder_When_Not_Running_In_Azure_Should_Return_Null()
        {
            Assert.Null(AzureFunctionsFileHelper.GetFunctionBaseFolder());
        }

        [Fact]
        public void GetAzureFunctionBaseFolder_When_Does_Not_Have_Home_EnvVar_Return_Null()
        {
            SetRunningInAzureEnvVars();
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureHomeEnvVarName, null);
            Assert.Null(AzureFunctionsFileHelper.GetFunctionBaseFolder());
        }

        [Fact]
        public void GetAzureFunctionBaseFolder_When_Running_In_Azure_Should_Return_Not_Null()
        {
            SetRunningInAzureEnvVars();
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureHomeEnvVarName, @"d:\Home");

            var actual = AzureFunctionsFileHelper.GetFunctionBaseFolder();
            Assert.NotEmpty(actual);

            var expected = $"d:\\Home{Path.DirectorySeparatorChar}site{Path.DirectorySeparatorChar}wwwroot";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetAzureFunctionBaseFolder_When_Running_In_Container_Should_Return_Not_Null()
        {
            const string pathInContainer = @"home/site/wwwroot";

            SetRunningInAzureEnvVars();
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureWebJobsScriptRootEnvVarName, pathInContainer);

            var actual = AzureFunctionsFileHelper.GetFunctionBaseFolder();
            Assert.NotEmpty(actual);
            Assert.Equal(pathInContainer, actual);
        }
    }
}