﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WorkLog.Models;
using System.Linq;
using WorkLog.Utils;
using System.Text.Json.Nodes;

namespace WorkLog.Data
{
    public class WorkLogAnswerContext : DbContext
    {
        public WorkLogAnswerContext(DbContextOptions<WorkLogAnswerContext> options)
            : base(options)
        {
        }

        public DbSet<WorkLog.Models.WorkLogAnswer> WorkLog { get; set; } = default!;

        /*
            Given Date, Email, ChannelId, Get All Answers of AnswerDTO modal. 
        */
        public List<WorkLogAnswerDTO> getAnswerDTOSOfDate(DateTime Date, string Email = "", int channelId = -1)
        {
            var inventories = WorkLog.
               FromSqlRaw($"SELECT * FROM WorkLog WHERE CONVERT(Date, Date) = CONVERT(Date, GETDATE()) AND Email = '{Email}' AND ChannelId = {channelId}")
               .ToList();

            List<WorkLogAnswerDTO> AnswerDTOS = new List<WorkLogAnswerDTO>();
            if (inventories.Count == 0)
                return new List<WorkLogAnswerDTO>();

            //change invetories into type of DTO
            for (int i = 0; i < inventories.Count; i++)
            {
                if (i == 0)
                {
                    AnswerDTOS.Add(new WorkLogAnswerDTO
                    {
                        Feeling = inventories[i].Feeling,
                        QuestionId = inventories[i].QuestionId,
                    });
                }
                // if this is first answer of question
                else if (inventories[i].AnswerId == 0)
                {
                    AnswerDTOS.Add(new WorkLogAnswerDTO
                    {
                        Answers = new List<string>() { inventories[i].Answer },
                        QuestionId = inventories[i].QuestionId
                    });
                }
                //already inserted into AnswerDTOS
                else
                {
                    AnswerDTOS[inventories[i].QuestionId]?.Answers?.Add(inventories[i].Answer);
                }
            }
            return AnswerDTOS;
        }

        /*
            Give start and count, get dates that has logs recorded on that date.
        */
        public List<DateTime> GetDateList(int start, int count, string Email = "", int channelId = -1)
        {
            try
            {
                FormattableString query = $"select Min([Date]) as [Date] from WorkLog where [ChannelId] = {channelId} and [Email] = '{Email}' group by CONVERT([Date], Date) order by [Date] offset {start} rows fetch first {count} rows only";
                return this.Database.SqlQueryRaw<DateTime>(query.ToString()).ToList();
            } catch (Exception e)
            {
                return new List<DateTime>();
            }
        }

        /*
            Concat All Answers between beginDate and endDate by Email and channelId for a word cloud. 
        */
        public string ConcatAllAnswersBetweenDateRange(DateTime beginDate, DateTime endDate,string Email = "", int channelId = -1)
        {
            try
            {
                FormattableString query = $"select STRING_AGG([Answer], ' ') as [Answer_Concat] from [WorkLog] where [ChannelId] = {channelId} and [Email] = '{Email}' And [Date] Between '{new DateTimeUtils().startTimeOfDate(beginDate).ToString()}' And '{new DateTimeUtils().endTimeOfDate(endDate).ToString()}'";
                var workLogSearchDTOS = this.Database.SqlQueryRaw<string>(query.ToString())
                    .ToList();

                return workLogSearchDTOS[0];
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        /*
            Get happy, sad, normal days between beginDate and endDate by Email and channelId. 
        */
        public List<int> GetFeelingCounts(DateTime beginDate, DateTime endDate, string Email = "", int channelId = -1)
        {
            try
            {
                List<int> FeelingCounts = new List<int>();
                int i;
                for(i = 0;i < 3;i++)
                {
                    FormattableString query = $"select Count([Id]) from [WorkLog] where [ChannelId] = {channelId} and [Email] = '{Email}' And [Date] Between '{new DateTimeUtils().startTimeOfDate(beginDate).ToString()}' And '{new DateTimeUtils().endTimeOfDate(endDate).ToString()}' And [QuestionId] = 0 And [Feeling] = {i}";
                    var averageFeeling = this.Database.SqlQueryRaw<int>(query.ToString())
                        .ToList();
                    FeelingCounts.Add(averageFeeling[0]);
                }

                return FeelingCounts;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}