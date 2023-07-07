package net.justonedev.chess.stormcloud.screenreader;

import java.awt.*;
import java.util.Scanner;

public class ScreenReader {
	
	static Scanner Input;
	
	static Robot r;
	static {
		try {
			r = new Robot();
		} catch (AWTException e) {
			e.printStackTrace();
		}
	}
	
	
	public static void main(String[] args)
	{
		Input = new Scanner(System.in);
		while(true)
		{
			String input = awaitInput();
			String ret;
			if(input.equals("getBoard")) ret = getBoard();
			else continue;
			print(ret);
		}
	}
	
	static String awaitInput()
	{
		return Input.nextLine();
	}
	
	static void print(String s)
	{
		System.out.println(s);
	}
	
	static String getBoard()
	{
		return "";
	}
	
}
