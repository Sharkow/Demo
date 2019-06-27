/* 3pi_RomeoAndJulietteDance - an application for the Pololu 3pi Robot
 *
 * This application uses the Pololu AVR C/C++ Library.  For help, see:
 * -User's guide: http://www.pololu.com/docs/0J20
 * -Command reference: http://www.pololu.com/docs/0J18
 *
 * Created: 4/25/2016 4:07:07 PM
 *  Author: Sharkow
 */

#include <pololu/3pi.h>
#include <math.h>

const unsigned baseBeatTime = 688;

static unsigned currentBeatTime;// = baseCountTime;
static float increaseSpeedCoeff;// = 1;
static int beepOn;

static void Beep()
{
	play_frequency(320, 100, 120);
}

static void BeepOpt()
{
	if (beepOn)
		Beep();
}

static void WaitHalfBeat()
{
	delay_ms(currentBeatTime/2);
}

static void BeepBeats(const unsigned beats)
{
	for (unsigned i = 0; i < beats; ++i)
	{
		BeepOpt();
		delay_ms(currentBeatTime);
	}
}

static void SetBeatTime(const unsigned beatTime)
{
	currentBeatTime = beatTime;
	increaseSpeedCoeff = (float)baseBeatTime / currentBeatTime;
}

static int AdjustedSpeed(const int speed)
{
	return round(speed * increaseSpeedCoeff);
}

static void FullTurnHalfBeat(const int left, const int stopAfter)
{
	const int speed = AdjustedSpeed(175);
	if (left) set_motors(-speed, speed);
	else      set_motors(speed, -speed);
	
	WaitHalfBeat();
	if (stopAfter)
		set_motors(0,0);
}

static void DoubleTurnPerBeat(const unsigned beats, const int left, const int stopAfter)
{
	const int speed = AdjustedSpeed(175);
	if (left) set_motors(-speed, speed);
	else      set_motors(speed, -speed);
	
	BeepBeats(beats);
	if (stopAfter)
		set_motors(0,0);
}

static void FullTurn2Beats(const int left, const int startOnStrongBeat, const int stopAfter, const int speedCorrection)
{
	const int speed = AdjustedSpeed(48);
	if (left) set_motors(-speed - speedCorrection, speed + 1 + speedCorrection);
	else      set_motors(speed + 3 + speedCorrection, -speed + 1 - speedCorrection);
	
	if (startOnStrongBeat)
		BeepBeats(2);
	else
	{
		WaitHalfBeat();
		BeepBeats(1);
		BeepOpt();
		WaitHalfBeat();
	}
	if (stopAfter)
		set_motors(0,0);	
}

static void FullTurn1Beat(const int left, const int startOnStrongBeat, const int stopAfter, const int speedCorrection)
{
	const int speed = AdjustedSpeed(89);
	if (left) set_motors(-speed - speedCorrection, speed + speedCorrection);
	else      set_motors(speed + speedCorrection, -speed - speedCorrection);
	
	if (startOnStrongBeat) BeepBeats(1);
	else
	{
		WaitHalfBeat();
		BeepOpt();
		WaitHalfBeat();
	}
	
	if (stopAfter)
		set_motors(0,0);
}

static void AroundModerate4Beats(const int left, const int stopAfter)
{
	//Похоже, левый-правый движки выдают разную скорость при одном инпуте...
	
	int leftSpeed, rightSpeed;
	if (left)
	{
		leftSpeed = AdjustedSpeed(32);
		rightSpeed = AdjustedSpeed(72);
	}
	else
	{
		leftSpeed = AdjustedSpeed(75);
		rightSpeed = AdjustedSpeed(31);
	}

	set_motors(leftSpeed, rightSpeed);
	
	BeepBeats(3);
	if (left) set_motors(leftSpeed+1, rightSpeed-3);
	else      set_motors(leftSpeed-3, rightSpeed+1);
	BeepBeats(1);
	
	if (stopAfter)
		set_motors(0,0);	
}

static void AroundModerate2AndHalfBeats(const int left, const int startOnStrongBeat, const int stopAfter)
{
	int leftSpeed, rightSpeed;
	if (left)
	{
		leftSpeed = AdjustedSpeed(38);
		rightSpeed = AdjustedSpeed(101);
	}
	else
	{
		leftSpeed = AdjustedSpeed(106);
		rightSpeed = AdjustedSpeed(37);
	}

	set_motors(leftSpeed, rightSpeed);
	if (startOnStrongBeat)
	{
		BeepBeats(2);
		WaitHalfBeat();
	}
	else
	{
		WaitHalfBeat();
		BeepBeats(2);
	}
	if (stopAfter)
		set_motors(0,0);
}

static void AroundModerate3AndHalfBeats(const int left, const int startOnStrongBeat, const int stopAfter)
{
	int leftSpeed, rightSpeed;
	if (left)
	{
		leftSpeed = AdjustedSpeed(32);
		rightSpeed = AdjustedSpeed(77);
	}
	else
	{
		leftSpeed = AdjustedSpeed(80);
		rightSpeed = AdjustedSpeed(31);
	}

	set_motors(leftSpeed, rightSpeed);
	if (startOnStrongBeat)
	{
		BeepBeats(3);
		WaitHalfBeat();
	}
	else
	{
		WaitHalfBeat();
		BeepBeats(3);
	}
	if (stopAfter)
		set_motors(0,0);
}

static void AroundModerate3Beats(const int left, const int startOnStrongBeat, const int stopAfter)
{
	int leftSpeed, rightSpeed;
	if (left)
	{
		leftSpeed = AdjustedSpeed(36);
		rightSpeed = AdjustedSpeed(87);
	}
	else
	{
		leftSpeed = AdjustedSpeed(93);
		rightSpeed = AdjustedSpeed(35);
	}

	set_motors(leftSpeed, rightSpeed);
	if (startOnStrongBeat) BeepBeats(2); else { WaitHalfBeat(); BeepBeats(1); WaitHalfBeat(); }
	if (left) set_motors(leftSpeed, rightSpeed - 1);
	else      set_motors(leftSpeed - 2, rightSpeed);
	if (startOnStrongBeat) BeepBeats(1); else { WaitHalfBeat(); BeepOpt(); WaitHalfBeat(); }
	
	if (stopAfter)
		set_motors(0,0);
}

static void AroundLarge8Beats(const int left, const int stopAfter)
{	
	int leftSpeed, rightSpeed;
	if (left)
	{
		leftSpeed = AdjustedSpeed(40);
		rightSpeed = AdjustedSpeed(59);
	}
	else
	{
		leftSpeed = AdjustedSpeed(62);
		rightSpeed = AdjustedSpeed(40);
	}
	set_motors(leftSpeed, rightSpeed);
	
	BeepBeats(7);
	if (left)
		set_motors(leftSpeed, rightSpeed-1);
	else
		set_motors(leftSpeed+1, rightSpeed);
	BeepBeats(1);
	if (stopAfter)
		set_motors(0,0);
}

static void AroundLarge7Beats(const int left, const int stopAfter)
{
	int leftSpeed, rightSpeed;
	if (left)
	{
		leftSpeed = AdjustedSpeed(41);
		rightSpeed = AdjustedSpeed(63);
	}
	else
	{
		leftSpeed = AdjustedSpeed(65);
		rightSpeed = AdjustedSpeed(40);
	}
	set_motors(leftSpeed, rightSpeed);
	BeepBeats(6);
	if (left)
		set_motors(leftSpeed, rightSpeed-1);
	else
		set_motors(leftSpeed+1, rightSpeed);
	BeepBeats(1);
	
	if (stopAfter)
		set_motors(0,0);
}

static void Around5Beats(const int left)
{
	int leftSpeed, rightSpeed;
	if (left)
	{
		leftSpeed = AdjustedSpeed(42);
		rightSpeed = AdjustedSpeed(72);
	}
	else
	{
		leftSpeed = AdjustedSpeed(76);
		rightSpeed = AdjustedSpeed(40);
	}
	set_motors(leftSpeed, rightSpeed);
	BeepBeats(4);
	if (left)
		set_motors(leftSpeed, rightSpeed-3);
	else
		;
	BeepBeats(1);
	
	set_motors(0,0);
}

static void AroundFast2Beats(const int left, const int stopAfter, const int leftCorrection)
{
	int leftSpeed, rightSpeed;
	if (left)
	{
		leftSpeed = AdjustedSpeed(55);
		rightSpeed = AdjustedSpeed(132);
	}
	else
	{
		leftSpeed = AdjustedSpeed(139);
		rightSpeed = AdjustedSpeed(52);
	}
	set_motors(leftSpeed + leftCorrection, rightSpeed);
	BeepBeats(2);
	
	if (stopAfter)
		set_motors(0,0);
}

static void EightLoop4Beats()
{
	set_motors(AdjustedSpeed(139), AdjustedSpeed(52));
	BeepBeats(2);
	set_motors(AdjustedSpeed(52), AdjustedSpeed(128));
	BeepBeats(2);
	set_motors(0,0);
}

static void StraghtModerate(const unsigned beats, const int back, const int startOnStrongBeat, const int stopAfter)
{
	int speed = AdjustedSpeed(48);
	if (back) set_motors(-speed, -speed + 1);
	else      set_motors(speed, speed - 1);
	if (startOnStrongBeat) BeepBeats(beats);
	else
	{
		for (unsigned i = 0; i < beats; ++i)
		{
			WaitHalfBeat();
			BeepOpt();
			WaitHalfBeat();
		}
	}
	if (stopAfter)
		set_motors(0,0);
}

static void StraghtManual(const unsigned beats, const int back, const int manualSpeed, const int stopAfter, const int leftCorrection)
{
	int speed = AdjustedSpeed(manualSpeed);
	if (back) set_motors(-speed * 1.03 -leftCorrection, -speed * 0.96);
	else      set_motors(speed * 1.03 + leftCorrection, speed * 0.96);
	BeepBeats(beats);
	if (stopAfter)
		set_motors(0,0);
}

static void StraghtPrecise(const unsigned time, const int back, const int manualSpeed, const int stopAfter, const int leftCorrection)
{
	int speed = AdjustedSpeed(manualSpeed);
	if (back) set_motors(-speed * 1.03 - leftCorrection, -speed * 0.96);
	else      set_motors(speed * 1.03 + leftCorrection, speed * 0.96);
	delay_ms(time);
	if (stopAfter)
		set_motors(0,0);
}

static void LeanSideHalfBeat(const int left, const int speedCorrection)
{
	int speed = AdjustedSpeed(42);
	if (left) set_motors(-speed - speedCorrection, speed + speedCorrection);
	else      set_motors(speed + 1 + speedCorrection, -speed - 2 - speedCorrection);
	WaitHalfBeat();
	set_motors(0,0);
}

static void LeanOneWheelHalfBeat(const int left, const int back, const int speedCorrection)
{
	const int speed = AdjustedSpeed(56);///left ? AdjustedSpeed(56) : AdjustedSpeed(56);
	if (left) set_motors(back ? -speed - speedCorrection : speed + speedCorrection, 0);
	else      set_motors(0, back ? -speed - speedCorrection : speed + speedCorrection);
	WaitHalfBeat();
		set_motors(0,0);
}

static void Turn90HalfBeat(const int left)
{
	int speed = AdjustedSpeed(47);
	if (left) set_motors(-speed, speed + 1);
	else      set_motors(speed + 2, -speed - 2);
	WaitHalfBeat();
	set_motors(0,0);
}

static void Turn180HalfBeat(const int left)
{
	int speed = AdjustedSpeed(88);
	if (left) set_motors(-speed, speed + 2);
	else      set_motors(speed + 1, -speed - 2);
	WaitHalfBeat();
	set_motors(0,0);
}

static void FromBackToForthAccelerate4Beats()
{
	BeepOpt();
	
	//1 такт - назад. Пол такта ускоряем, пол останавливаемся
	const int maxBackSpeed = 72;
	const int startBackSpeed = 12;
	const unsigned halfBeatTime = currentBeatTime/2;
	const unsigned speedUpBackIterations = halfBeatTime/10;
	const float backIncrement = ((float)maxBackSpeed - startBackSpeed) / speedUpBackIterations;
	float speed = 0;
	for (unsigned time = 0; time < halfBeatTime; time+=10)
	{
		speed += backIncrement;
		set_motors(-speed, -speed);
		delay_ms(10);
		if (halfBeatTime - time < 10)
		{
			delay_ms(halfBeatTime-time);
			break;
		}
	}
	
	const unsigned slowDownBackIterations = halfBeatTime/10;
	const float backDecrement = speed/slowDownBackIterations;
	
	for (unsigned time = 0; time < halfBeatTime; time+=10)
	{
		speed -= backDecrement;
		set_motors(-speed, -speed);
		delay_ms(10);
		if (halfBeatTime - time < 10)
		{
			delay_ms(halfBeatTime-time);
			break;
		}
	}
	
	BeepOpt();
	
	//1.5 такта рывок вперед
	const int maxForthSpeed = 156;
	const int startForthSpeed = 6;
	const unsigned speedUpForthIterations = currentBeatTime/10;
	const float forthIncrement = ((float)maxForthSpeed - startForthSpeed) / speedUpForthIterations;
	speed = startForthSpeed;
	for (unsigned time = 0; time < currentBeatTime; time+=10)
	{
		speed += forthIncrement;
		set_motors(speed + 1, speed - 1);
		delay_ms(10);
		if (currentBeatTime - time < 10)
		{
			delay_ms(currentBeatTime-time);
			break;
		}
	}
	
	BeepOpt();
	
	const unsigned slowDownForthIterations = halfBeatTime/10;
	const float forthDecrement = speed/slowDownForthIterations;
	
	for (unsigned time = 0; time < halfBeatTime; time+=10)
	{
		speed -= forthDecrement;
		set_motors(speed, speed);
		delay_ms(10);
		if (halfBeatTime - time < 10)
		{
			delay_ms(halfBeatTime-time);
			break;
		}
	}
	
	//1.5 такта откат назад
	const int maxLastBackSpeed = 70;
	const int startLastBackSpeed = 6;
	const unsigned lastSpeedUpBackIterations = halfBeatTime/10;
	const float lastBackIncrement = ((float)maxLastBackSpeed - startLastBackSpeed) / lastSpeedUpBackIterations;
	speed = startLastBackSpeed;
	for (unsigned time = 0; time < halfBeatTime; time+=10)
	{
		speed += lastBackIncrement;
		set_motors(-speed - 1, -speed);
		delay_ms(10);
		if (halfBeatTime - time < 10)
		{
			delay_ms(halfBeatTime-time);
			break;
		}
	}
	
	BeepOpt();
	WaitHalfBeat();
	
	const unsigned lastSlowDownBackIterations = halfBeatTime/10;
	const float lastBackDecrement = speed/lastSlowDownBackIterations;
	
	for (unsigned time = 0; time < halfBeatTime; time+=10)
	{
		speed -= lastBackDecrement;
		set_motors(-speed, -speed);
		delay_ms(10);
		if (halfBeatTime - time < 10)
		{
			delay_ms(halfBeatTime-time);
			break;
		}
	}
	
	set_motors(0,0);
}

static void StraightAccelerarePrecise(const int back, const unsigned time, const int startSpeed, const int maxSpeed, const int leftCorrection)
{	
	const unsigned speedUpTime = round(time*0.3);
	const unsigned speedUpIterations = speedUpTime/10;
	const float speedIncrement = ((float)maxSpeed - startSpeed) / speedUpIterations;
	float speed = startSpeed;
	for (unsigned time = 0; time < speedUpTime; time+=10)
	{
		speed += speedIncrement;
		if (back) set_motors(-speed - 1 - leftCorrection, -speed); else set_motors(speed + 1 + leftCorrection, speed);
		delay_ms(10);
		if (speedUpTime - time < 10)
		{
			delay_ms(speedUpTime-time);
			break;
		}
	}
	
	//const unsigned staightTime = speedUpTime;
	//delay_ms(staightTime);
	
	const unsigned slowDownTime = round(time*0.7);
	const unsigned slowDownIterations = slowDownTime/10;
	const float speedDecrement = speed/slowDownIterations;
	
	for (unsigned time = 0; time < slowDownTime; time+=10)
	{
		speed -= speedDecrement;
		if (back) set_motors(-speed - 1 - leftCorrection, -speed); else set_motors(speed + 1 + leftCorrection, speed);
		delay_ms(10);
		if (slowDownTime - time < 10)
		{
			delay_ms(slowDownTime-time);
			break;
		}
	}
	set_motors(0,0);
}

static void Dance()
{
	//Цифра обозначает момент удара на долю
	SetBeatTime(702);
								//1
	AroundLarge7Beats(0, 1);	//2
								//3
								//4
								//1
								//2
								//3
								//4
	BeepOpt(); WaitHalfBeat(); LeanSideHalfBeat(1, 0);//1
	BeepOpt(); WaitHalfBeat(); LeanSideHalfBeat(0, -2);//2
	BeepBeats(1);				//3
	StraghtModerate(2, 1, 1, 1);//4
								//1
	BeepBeats(1);				//2
	FullTurn2Beats(0, 1, 1, 0);	//3
								//4
	SetBeatTime(708);
	BeepBeats(3);				//1
								//2
								//3
    WaitHalfBeat(); FullTurnHalfBeat(0, 0);//4
	DoubleTurnPerBeat(1, 0, 1);	//1
	BeepBeats(1);				//2
	AroundModerate3Beats(1, 1, 1);//3
								//4
	SetBeatTime(690);
								//1
	BeepOpt(); WaitHalfBeat(); StraghtPrecise(currentBeatTime/2, 0, 56, 0, 0);//2
	StraghtManual(1, 0, 56, 1, 0);//3
	BeepOpt(); WaitHalfBeat(); LeanSideHalfBeat(0, -5);//4
	SetBeatTime(670);
	BeepOpt(); LeanSideHalfBeat(1, 11); WaitHalfBeat();//1
	FromBackToForthAccelerate4Beats();//2 Love song
								//3	That he made
								//4
								//1
	FullTurn1Beat(0, 1, 1, 0);	//2
	SetBeatTime(690);
	BeepOpt(); WaitHalfBeat(); LeanSideHalfBeat(1, 0);//3 a streetlight
	BeepBeats(1);				//4
	BeepOpt(); WaitHalfBeat(); StraghtPrecise(currentBeatTime/2, 0, 36, 0, 0);//1
	StraghtManual(2, 0, 36, 1, 0);//2
								//3
	SetBeatTime(690);
	BeepOpt(); WaitHalfBeat(); LeanSideHalfBeat(0, 0);//4 something like
	BeepBeats(2);				//1//SetBeatTime(690); BeepBeats(1); 
								//2
	BeepOpt(); WaitHalfBeat(); StraghtPrecise(currentBeatTime * 0.4, 0, 62, 1, 0); delay_ms(currentBeatTime * 0.1);//3 and me babe
	BeepOpt(); StraghtPrecise(currentBeatTime * 0.4, 0, 62, 1, 0); delay_ms(currentBeatTime * 0.1); WaitHalfBeat();//4
	BeepOpt(); WaitHalfBeat(); FullTurn2Beats(1, 0, 1, 0);//2.5
	LeanSideHalfBeat(0, 0); BeepOpt();//WaitHalfBeat(); BeepOpt();	//3
	StraghtManual(1, 0, 40, 0, 0);//4
	BeepOpt(); StraghtPrecise(currentBeatTime/2, 0, 40, 1, 0); LeanSideHalfBeat(1, 7);//1//SetBeatTime(690);
	BeepBeats(2);				//2
								//3
	AroundFast2Beats(0, 1, 0);	//4 it's Romeo
								//1
	SetBeatTime(698);
	BeepOpt(); WaitHalfBeat(); Turn90HalfBeat(0);//2
	BeepOpt(); WaitHalfBeat(); Turn90HalfBeat(0);//3
	BeepOpt(); WaitHalfBeat(); StraghtPrecise(currentBeatTime/2, 0, 36, 0, 0);//4
	StraghtManual(2, 0, 36, 1, 0);//1
								//2
	BeepOpt(); WaitHalfBeat(); Turn180HalfBeat(0);//3 window
	SetBeatTime(684);
	BeepBeats(1);				//4
	BeepOpt(); LeanSideHalfBeat(1, 0); LeanSideHalfBeat(0, 0);//1
	BeepOpt(); WaitHalfBeat(); StraghtPrecise(currentBeatTime/2, 0, 40, 0, -1);//2
	StraghtManual(1, 0, 40, 0, -1);//3 my boyfriend's
	EightLoop4Beats();			//4
								//1
								//2
								//3 come around
	BeepBeats(2);				//4 here
								//1
	FullTurn2Beats(0, 1, 1, -4);	//2
								//3 like that
	BeepBeats(2);				//4
								//1 //SetBeatTime(684);
	BeepOpt(); WaitHalfBeat(); LeanOneWheelHalfBeat(0, 1, -2);//2
	BeepOpt(); LeanOneWheelHalfBeat(1, 1, 4); WaitHalfBeat();//3 what
	Around5Beats(1);			//4
								//1
								//2
								//3
								//4
    BeepOpt(); WaitHalfBeat(); FullTurn1Beat(0, 0, 0, 0);//1.5 Juliette...
	FullTurn1Beat(0, 0, 0, 0);	//2.5
	FullTurn1Beat(0, 0, 0, 0);	//3.5
	FullTurn1Beat(0, 0, 1, -8);	//4.5 the dice was
	StraightAccelerarePrecise(0, currentBeatTime*3, 34, 220, 4);//1 loaded
								//2
								//3
	WaitHalfBeat();				//4
	SetBeatTime(670);
	BeepOpt(); Turn180HalfBeat(1); StraightAccelerarePrecise(0, currentBeatTime*2.5, 34, 220, 5);//1
								//2
								//3
	BeepOpt(); WaitHalfBeat(); Turn180HalfBeat(0);//4 and you
	AroundFast2Beats(1, 0, -4);	//1 exploded
								//2 in
	AroundFast2Beats(0, 1, -4);	//3 my heart
								//4
	FullTurn1Beat(1, 1, 1, 0);	//1 forget
	BeepOpt(); WaitHalfBeat(); AroundModerate3Beats(0, 0, 1);//2.5
								//3.5
								//4.5
	WaitHalfBeat();				//1
	BeepBeats(1);				//2
	FullTurn1Beat(0, 1, 1, -8);	//3 movie song
	BeepOpt(); WaitHalfBeat(); StraightAccelerarePrecise(0, currentBeatTime*2.5, 20, 190, 4);//4
								//1
								//2
	AroundFast2Beats(0, 1, 0);	//3 realize
								//4
	SetBeatTime(690);
	WaitHalfBeat(); Turn180HalfBeat(1);//1 just SetBeatTime(670);
	BeepOpt(); WaitHalfBeat();	StraghtModerate(1, 0, 0, 1);//2.5 time
	LeanSideHalfBeat(1, 0);		//3. was wrong
	BeepOpt(); LeanSideHalfBeat(0, 0); LeanSideHalfBeat(0, 0);//4
	BeepOpt(); LeanSideHalfBeat(1, 0); WaitHalfBeat();//1
	SetBeatTime(699);
	AroundLarge7Beats(0, 0);	//2 Juliette...
								//3
								//4
								//1
								//2
								//3
								//4
	BeepBeats(8);				//1
								//2
								//3
								//4
								//1
								//2
								//3
								//4
	Turn90HalfBeat(1); Turn90HalfBeat(1);//1
								
	
	
	
}

int main()
{
	clear();
	currentBeatTime = baseBeatTime;
	increaseSpeedCoeff = 1;
	beepOn = 1;
	
	SetBeatTime(699);
	//SetBeatTime(684);//temp
	
	while (!button_is_pressed(BUTTON_B))
		delay_ms(100);
		
	BeepBeats(4);
	
	//////////////////////////////////////////////////////////
	
	//beepOn = 0;
	Dance();
	
	while(1) { delay_ms(10000); }//DON'T REMOVE
}
