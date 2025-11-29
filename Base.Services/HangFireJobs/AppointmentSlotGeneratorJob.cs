using Base.DAL.Contexts;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Microsoft.EntityFrameworkCore;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.HangFireJobs
{
    public class AppointmentSlotGeneratorJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _appDbContext;

        public AppointmentSlotGeneratorJob(IUnitOfWork unitOfWork, AppDbContext appDbContext)
        {
            _unitOfWork = unitOfWork;
            _appDbContext = appDbContext;
        }

        public async Task GenerateMonthlySlotsAsync()
        {
            var scheduleRepo = _unitOfWork.Repository<ClinicSchedule>();
            var slotRepo = _unitOfWork.Repository<AppointmentSlot>();
            var allSchedules = await scheduleRepo.ListAllAsync();
            var now = DateTime.UtcNow; // وقت النظام

            var startDate = now.Date;
            var endDate = startDate.AddMonths(1);

            foreach (var schedule in allSchedules)
            {
                // لكل يوم خلال الشهر
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != schedule.Day)
                        continue;

                    var dayStart = date.Add(schedule.StartTime);
                    var dayEnd = date.Add(schedule.EndTime);

                    for (var slotStart = dayStart;
                         slotStart < dayEnd;
                         slotStart = slotStart.AddMinutes(schedule.SlotDurationMinutes))
                    {
                        var slotEnd = slotStart.AddMinutes(schedule.SlotDurationMinutes);

                        var slotStartTimeSpan = slotStart.TimeOfDay;
                        var spec = new BaseSpecification<AppointmentSlot>(s =>
    s.ClinicScheduleId == schedule.Id &&
    s.StartTime == slotStartTimeSpan);
                        var existsindb = await slotRepo.CountAsync(spec);
                        //                    var existsinmemory = _appDbContext.Set<AppointmentSlot>().Any(s =>
                        //s.ClinicScheduleId == schedule.Id &&
                        //s.StartTime == slotStartTimeSpan);
                        var existsinmemory = _appDbContext.AppointmentSlot.Any(s => s.ClinicScheduleId == schedule.Id && s.StartTime == slotStartTimeSpan);

                        if (existsindb < 1 && !existsinmemory)
                        {
                            var newSlot = new AppointmentSlot
                            {
                                ClinicScheduleId = schedule.Id,
                                Date = slotStart.Date,
                                StartTime = slotStart.TimeOfDay,
                                EndTime = slotEnd.TimeOfDay,
                                IsBooked = false
                            };

                            //Console.WriteLine($"{_appDbContext.Entry(newSlot).State}\t Date = {newSlot.Date} , StartTime = {newSlot.StartTime} , EndTime = {newSlot.EndTime}  ");

                            await slotRepo.AddAsync(newSlot);

                        }
                    }
                }
            }
            await _unitOfWork.CompleteAsync();

        }
    }

}
