﻿using ClassroomReservation.Client;
using ClassroomReservation.Main;
using ClassroomReservation.Other;
using ClassroomReservation.Resource;
using ClassroomReservation.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClassroomReservation.Reservation
{
    public delegate void OnReservationSuccess(ReservationItem item);

    public partial class ReservationWindow : Window
    {
        public OnReservationSuccess onReservationSuccess { set; private get; }

        public ReservationWindow(bool isUserMode)
        {
            InitializeComponent();

            if (isUserMode) {
                calendar.BlackoutDates.Add(new CalendarDateRange(DateTime.MinValue, DateTime.Today.AddDays(-1)));
                calendar.BlackoutDates.Add(new CalendarDateRange(DateTime.Today.AddDays(7), DateTime.MaxValue));
            }
            calendar.SelectedDate = DateTime.Now;

            calendar.PreviewMouseUp += new MouseButtonEventHandler((sender, e) => Mouse.Capture(null));

            OK_Button.Click += new RoutedEventHandler(Reserve);
            Cancel_Button.Click += new RoutedEventHandler((sender, e) => this.Close());
            
            timeSelectControl.onTimeSelectChanged = OnTimeSelectChanged;
            
            classroomSelectControl.onClassroomSelectChanged = OnClassroomSelectChanged;

            EnableInputUserData(false);

            try {
                DateTime selectedDate = ReservationStatusPerDay.nowSelectedStatusControl.date;
                int[] selectedClasstimeRow = ReservationStatusPerDay.nowSelectedColumn;
                int selectedClassroomRow = ReservationStatusPerDay.nowSelectedRow - 2;

                calendar.SelectedDate = selectedDate;
                timeSelectControl.SetSelectedTime(selectedClasstimeRow);
                classroomSelectControl.SetSelectedClassroom(selectedClassroomRow);
                
                EnableInputUserData(true);
            } catch (Exception ex) {

            }
        }

        private void Calendar_OnSelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            timeSelectControl.enable(true);
            timeSelectControl.ResetSelection();

            classroomSelectControl.enable(true);
            classroomSelectControl.ResetSelection();

            EnableInputUserData(false);
        }

        private void Reserve(object sender, RoutedEventArgs e) {
            if (calendar.SelectedDate == null)
                MessageBox.Show("날짜를 선택해 주세요", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (!timeSelectControl.HasSeletedTime())
                MessageBox.Show("시간을 선택해 주세요", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (!classroomSelectControl.HasSelectedClassroom())
                MessageBox.Show("강의실을 선택해 주세요", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (nameTextBox.Text.Equals(""))
                MessageBox.Show("이름을 입력해 주세요", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (nameTextBox.Text.Length > 100)
                MessageBox.Show("이름은 100자를 넘을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (Essential.hasSpecialChar(nameTextBox.Text))
                MessageBox.Show("이름에 특수문자(\" \' ; : \\ / + = * # |)및 query구문(union, select 등)을 넣을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (numberTextBox.Text.Equals(""))
                MessageBox.Show("연락처를 입력해 주세요", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (numberTextBox.Text.Length > 100)
                MessageBox.Show("연락처는 100자를 넘을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (Essential.hasSpecialChar(numberTextBox.Text))
                MessageBox.Show("연락처에 특수문자(\" \' ; : \\ / + = * # |)및 query구문(union, select 등)을 넣을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (contentTextBox.Text.Equals(""))
                MessageBox.Show("예약 내용을 입력해 주세요", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (contentTextBox.Text.Length > 1000)
                MessageBox.Show("예약 내용은 1000자를 넘을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (Essential.hasSpecialChar(contentTextBox.Text))
                MessageBox.Show("예약 내용에 특수문자(\" \' ; : \\ / + = * # |)및 query구문(union, select 등)을 넣을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (passwordTextBox.Password.Equals(""))
                MessageBox.Show("비밀번호을 입력해 주세요", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (passwordTextBox.Password.Length > 100)
                MessageBox.Show("비밀번호는 100자를 넘을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (Essential.hasSpecialChar(passwordTextBox.Password))
                MessageBox.Show("비밀번호에 특수문자(\" \' ; : \\ / + = * # |)및 query구문(union, select 등)을 넣을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (Essential.hasKorean(passwordTextBox.Password))
                MessageBox.Show("비밀번호에 한글을 넣을 수 없습니다", "예약 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
            else {
                DateTime startDate = calendar.SelectedDates[0];
                DateTime endDate = calendar.SelectedDates[calendar.SelectedDates.Count - 1];
                int[] time = timeSelectControl.GetSelectedTime();
                string classroom = classroomSelectControl.GetSelectedClassroom();

                string name = nameTextBox.Text;
                string contact = numberTextBox.Text;
                string content = contentTextBox.Text;
                string password = LoginClient.EncryptString(passwordTextBox.Password);
                Logger.logNext(passwordTextBox.Password);
                
                ReservationItem item = new ReservationItem(startDate, endDate, time[0], time[1], classroom, name, contact, content, password);
                
                MessageBoxResult result = MessageBox.Show(item.ToString() + "이 맞습니까?", "예약 하기", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes) {
                    overlapAll.Visibility = Visibility.Visible;
                    //try {
                    //    ServerClient.getInstance().reservationAdd(item);
                    //    overlapAll.Visibility = Visibility.Hidden;
                    //    onReservationSuccess?.Invoke(item);
                    //    Close();
                    //} catch (ServerResult ex) {
                    //    overlapAll.Visibility = Visibility.Hidden;
                    //    MessageBox.Show("알 수 없는 오류가 발생해서 예약에 실패했습니다.", "예약 하기", MessageBoxButton.OK, MessageBoxImage.Error);
                    //}
                }
            }
        }

        private void EnableInputUserData(bool enable) {
            if (enable) {
                overlapRectangle.Visibility = Visibility.Hidden;
            } else {
                overlapRectangle.Visibility = Visibility.Visible;
            }
        }

        private void OnTimeSelectChanged(int[] nowSelectedTime, bool hasBeforeSelect) {
            classroomSelectControl.selectiveEnable(ServerClient.getInstance().checkClassroomStatusByClasstime(
                    calendar.SelectedDates[0],
                    calendar.SelectedDates[calendar.SelectedDates.Count - 1],
                    nowSelectedTime[0],
                    nowSelectedTime[1]
            ));

            if (hasBeforeSelect) {
                timeSelectControl.enable(true);
                classroomSelectControl.ResetSelection();
                EnableInputUserData(false);
            } else {
                if (classroomSelectControl.HasSelectedClassroom() && timeSelectControl.HasSeletedTime())
                    EnableInputUserData(true);
            }
        }

        private void OnClassroomSelectChanged(string nowSelectedClassroom, bool hasBeforeSelect) {
            timeSelectControl.selectiveEnable(ServerClient.getInstance().checkClasstimeStatusByClassrom(
                    calendar.SelectedDates[0],
                    calendar.SelectedDates[calendar.SelectedDates.Count - 1],
                    nowSelectedClassroom
            ));

            if (hasBeforeSelect) {
                classroomSelectControl.enable(true);
                timeSelectControl.ResetSelection();
                EnableInputUserData(false);
            } else {
                if (classroomSelectControl.HasSelectedClassroom() && timeSelectControl.HasSeletedTime())
                    EnableInputUserData(true);
            }
        }

        //MouseLeftButtonDown="onMouseLeftBtnDown" MouseEnter="onMouseEnter" MouseLeftButtonUp="OnMouseLeftBtnUp"
    }
}
